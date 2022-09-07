using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Ext2
{
    class Ext2FsBuilder : Ext2FsBase, IFilesystemBuilder
    {
        uint DiskBlockSize;
        uint INodeIndex = 2;
        uint MaxBlockGroupCount = 0;
        uint DataBlockIndex = 0x13E;

        private List<BuilderDirectory> Dirs = new List<BuilderDirectory>();

        public Ext2FsBuilder(uint DiskBlockSize) : base(new byte[DiskBlockSize])
        {
            this.DiskBlockSize = DiskBlockSize;
            Fill(0x00);

            InitSuperblock();
            InitBlockGroups();
            InitBlockBitmapTables();
            InitINodeBitmapTables();
        }

        public void InitSuperblock()
        {
            Superblock.LogBlockSize = 0; // BlockSize => 0x400
            Superblock.LogClusterSize = 0;
            Superblock.BlocksCount = Convert.ToUInt32(getLength() / Superblock.BlockSize);
            Superblock.BlocksPerGroup = 0x2000; // TODO: 8 * Superblock.BlockSize (bitmap bits count)
            Superblock.CheckInterval = 0x9a7ec800;
            Superblock.ClustersPerGroup = 0x2000;
            Superblock.CreatorOS = 0x00;
            Superblock.DefaultReservedGid = 0x00;
            Superblock.DefaultReservedUid = 0x00;
            Superblock.Errors = 0x00;
            Superblock.FirstDataBlock = 0x01;
            Superblock.FreeBlocksCount = Superblock.BlocksCount - 1;
            Superblock.INodesCount = 0x2720; // TODO: Calc
            Superblock.FreeINodeCount = Superblock.INodesCount;
            Superblock.InodesPerGroup = 0x9c8;
            Superblock.LastCheckTime = DateTime.Now;
            Superblock.WTime = 0;
            Superblock.Magic = 0xef53;
            Superblock.MaxMountCount = 20;
            Superblock.MinorRevisionLevel = 0;
            Superblock.MountCount = 0;
            Superblock.MTime = 0;
            Superblock.RevLevel = 0;
            Superblock.RootBlocksCount = 0x666;
            Superblock.State = 1;
        }

        private void InitBlockGroups()
        {
            MaxBlockGroupCount = Convert.ToUInt32(getLength() / Superblock.BlockSize / Superblock.BlocksPerGroup);

            for (uint i = 0; i < MaxBlockGroupCount; i++)
            {
                var BG = GetBlockGroup(i);

                BG.BlockBitmapLo = 0x03 + i * 0x2000;
                BG.INodeBitmapLo = 0x04 + i * 0x2000;
                BG.INodeTableLo = 0x05 + i * 0x2000;
                BG.UsedDirsCountLo = 0;
                BG.FreeINodesCountLo = Superblock.InodesPerGroup;
            }
        }

        private void InitBlockBitmapTables()
        {
            // Zero = Free
        }

        private void InitINodeBitmapTables()
        {
            // Zero = Free
        }

        /// <summary>
        /// Get builded filesystem image
        /// </summary>
        /// <returns></returns>
        public byte[] GetFilesystemImage()
        {
            foreach(var D in Dirs)
            {
                SetNodeBlockContent(D.Node, D.Content);
                D.Node.LinksCount = Convert.ToUInt32(D.Entries.Count + 2);
            }
            return getPacket();
        }

        private BuilderDirectory GetParentDirectory(string Path)
        {
            var Parent = Universal.Helper.FsHelper.GetParentDirPath(Path);

            foreach(var D in Dirs)
            {
                if(D.Path == Parent)
                    return D;
            }

            return null;
        }

        private Types.ExtINode CreateNewINode(Types.ExtINodeType Type, uint User, uint Group, uint Mode)
        {
            var Index = INodeIndex++;
            var INode = GetINode(Index);
            INode.GID = Group;
            INode.UID = User;
            INode.Mode = Mode | Convert.ToUInt32(Type);
            INode.CTime = Universal.Helper.FsHelper.ConvertToUnixTimestamp(DateTime.Now);
            INode.MTime = INode.CTime;
            INode.ATime = INode.CTime;
            INode.DTime = 0;
            INode.BlocksLo = 0;
            INode.Block.Fill(0u);
            INode.LinksCount = 1;

            {
                // Mark inode as used
                var BG = GetBlockGroupByNodeId(Index);
                var INodeBitmapTableOffset = BG.INodeBitmapLo * Superblock.BlockSize;
                var Idx = BG.GetLocalNodeIndex(Index, Superblock.InodesPerGroup);

                var ByteOffset = INodeBitmapTableOffset + Idx / 8;
                var BitOffset = Convert.ToInt32(Idx % 8);

                var RawBitmapValue = ReadByte(ByteOffset);
                RawBitmapValue |= Convert.ToByte(1 << BitOffset);
                WriteByte(ByteOffset, RawBitmapValue);
            }

            Superblock.FreeINodeCount--;
            return INode;
        }

        private void AddNestedNode(string Path, Func<Types.ExtINode> NodeGetter)
        {
            var Parent = GetParentDirectory(Path);
            if (Parent != null)
            {
                var N = NodeGetter();
                Parent.Entries.Add(new Types.ExtDirectoryEntry(Convert.ToUInt32(N.Index), N.NodeType, Universal.Helper.FsHelper.GetName(Path)));

                var BG = GetBlockGroupByNodeId(N.Index);
                if(N.NodeType == Types.ExtINodeType.DIR)
                {
                    BG.UsedDirsCountLo++;
                    BG.FreeINodesCountLo--;

                    Dirs.Add(new BuilderDirectory(Path, N));
                }
            }
            else
                throw new InvalidOperationException("No parent dir!..");
        }

        /// <summary>
        /// Create block device
        /// </summary>
        /// <param name="Path">Path to block device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Block(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () =>
            {
                var N = CreateNewINode(Types.ExtINodeType.BLOCK, User, Group, Mode);
                N.Block[0] = (Major & 0xFF) | ((Minor & 0xFF) << 8);
                return N;
            });
        }

        /// <summary>
        /// Create char device
        /// </summary>
        /// <param name="Path">Path to char device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Char(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () =>
            {
                var N = CreateNewINode(Types.ExtINodeType.CHAR, User, Group, Mode);
                N.Block[0] = (Major & 0xFF) | ((Minor & 0xFF) << 8);
                return N;
            });
        }

        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="Path">Path to directory (parent dir must exists)</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Directory(string Path, uint User, uint Group, uint Mode)
        {
            if (Path == "/")
            {
                if (Dirs.Count == 0)
                {
                    var N = CreateNewINode(Types.ExtINodeType.DIR, User, Group, Mode);
                    Dirs.Add(new BuilderDirectory(Path, N));

                    INodeIndex += 12; // skip reserved items
                }
                else 
                    throw new ArgumentException("Cannot create new root node");
            }
            else
                AddNestedNode(Path, () => CreateNewINode(Types.ExtINodeType.DIR, User, Group, Mode));
        }

        /// <summary>
        /// Create fifo
        /// </summary>
        /// <param name="Path">Path to fifo</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Fifo(string Path, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => CreateNewINode(Types.ExtINodeType.FIFO, User, Group, Mode));
        }

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <param name="Content">File content</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () =>
            {
                var N = CreateNewINode(Types.ExtINodeType.REG, User, Group, Mode);
                SetNodeBlockContent(N, Content);

                return N;
            });
        }

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="Path">Path to socket</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Socket(string Path, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => CreateNewINode(Types.ExtINodeType.SOCK, User, Group, Mode));
        }

        /// <summary>
        /// Create symlink
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <param name="Target">Target path</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () =>
            {
                var N = CreateNewINode(Types.ExtINodeType.LINK, User, Group, Mode);

                if(Target.Length <= 60)
                    N.SetText(Target);
                else
                    SetNodeBlockContent(N, UTF8Encoding.UTF8.GetBytes(Target));

                return N;
            });
        }

        uint GetNewBlock()
        {
            var Index = DataBlockIndex++;
            var BG = GetBlockGroupByNodeId(Index);

            /*
            var BlockOffset = Index * Superblock.BlockSize;
            if(BlockOffset >= getLength())
            {
                Array.Resize(ref Raw, Convert.ToInt32(Raw.Length + DiskBlockSize));

                // TODO: Update BlockGroupTable, Superblock
            }
            */

            {
                // Mark block as used
                var BlockBitmapTableOffset = BG.BlockBitmapLo * Superblock.BlockSize;
                var Idx = BG.GetLocalNodeIndex(Index, Superblock.BlocksPerGroup);

                var ByteOffset = BlockBitmapTableOffset + Idx / 8;
                var BitOffset = Convert.ToInt32(Idx % 8);

                var RawBitmapValue = ReadByte(ByteOffset);
                RawBitmapValue |= Convert.ToByte(1 << BitOffset);
                WriteByte(ByteOffset, RawBitmapValue);
            }
            return Index;
        }


        void AddBlockToNode(Types.ExtINode Node, uint Block)
        {
            var Index = Node.BlocksLo / 2;
            if(Index < 12)
            {
                Node.UpdateBlockByIndex(Index, Block);
            }
            else if(Index < 12 + (Superblock.BlockSize / 4))
            {
                uint Offset = Index - 12;
                // One-level indirect table
                if (Offset == 0)
                    Node.UpdateBlockByIndex(12, GetNewBlock());// Create new block...

                uint IndBlock = Node.Block[12];
                FillDirectBlockTable(IndBlock, Offset, Block);
            }
            else if (Index < 12 + (Superblock.BlockSize / 4) + (Superblock.BlockSize / 4) * (Superblock.BlockSize / 4))
            {
                uint Offset = Index - 12 - (Superblock.BlockSize / 4);

                // Second-level indirect table
                if (Offset == 0)
                    Node.UpdateBlockByIndex(13, GetNewBlock());// Create new block...

                uint IndBlock = Node.Block[13];
                FillIndirectBlockTable(IndBlock, Offset, 1, Block);
            }
            else
            {
                uint Offset = Index - 12 - (Superblock.BlockSize / 4) - (Superblock.BlockSize / 4) * (Superblock.BlockSize / 4);

                // Third-level indirect table
                if (Offset == 0)
                    Node.UpdateBlockByIndex(14, GetNewBlock());// Create new block...

                uint IndBlock = Node.Block[14];
                FillIndirectBlockTable(IndBlock, Offset, 2, Block);
            }

            Node.BlocksLo += 2;
        }

        uint GetBlockInIUndirectTableIndex(uint Level)
        {
            uint Res = Superblock.BlockSize / 4;
            for (uint i = 0; i < Level - 1; i++)
            {
                Res *= Superblock.BlockSize / 4;
            }
            return Res;
        }

        void FillIndirectBlockTable(uint IndirectBlock, uint Offset, uint Level, uint Block)
        {
            long BlockOffset = IndirectBlock * Superblock.BlockSize;
            uint BlocksInIndex = GetBlockInIUndirectTableIndex(Level);

            uint Index = Offset / BlocksInIndex;
            uint NestedBlockOffset = Offset % BlocksInIndex;

            uint IndBlock = 0;
            if (NestedBlockOffset == 0)
            {
                IndBlock = GetNewBlock();
                WriteUInt32(BlockOffset + Index * 4, IndBlock);
            }
            else
                IndBlock = ReadUInt32(BlockOffset + Index * 4);

            if (Level > 1)
                FillIndirectBlockTable(IndBlock, NestedBlockOffset, Level - 1, Block);
            else
                FillDirectBlockTable(IndBlock, NestedBlockOffset, Block);
        }

        void FillDirectBlockTable(uint IndirectBlock, uint Offset, uint Block)
        {
            long BlockOffset = IndirectBlock * Superblock.BlockSize;
            WriteUInt32(BlockOffset + Offset * 4, Block);
        }

        void SetNodeBlockContent(Types.ExtINode Node, byte[] Content)
        {
            long Offset = 0;
            Node.SizeLo = Convert.ToUInt32(Content.Length);

            while (Offset < Content.Length)
            {
                var ToSave = Content.Length - Offset;
                var BlockSize = (ToSave > Superblock.BlockSize) ? Superblock.BlockSize : ToSave;

                var Block = GetNewBlock();
                long BlockOffset = Block * Superblock.BlockSize;
                var ToWrite = Content.ReadArray(Offset, BlockSize);
                WriteArray(BlockOffset, ToWrite, ToWrite.Length);

                AddBlockToNode(Node, Block);

                Offset += BlockSize;
            }
        }

        class BuilderDirectory
        {
            public Types.ExtINode Node;
            public string Path;

            public BuilderDirectory(string Path, Types.ExtINode Node)
            {
                this.Path = Path;
                this.Node = Node;
            }

            public List<Types.ExtDirectoryEntry> Entries = new List<Types.ExtDirectoryEntry>();

            public byte[] Content
            {
                get
                {
                    var Temp = new List<byte>();
                    foreach(var E in Entries)
                        Temp.AddRange(E.getPacket());

                    var TargetSize = Math.Max(0x400, Temp.Count.GetAligned(0x400));

                    var Res = new byte[TargetSize];
                    Res.WriteArray(0, Temp.ToArray(), Temp.Count);
                    return Res;
                }
            }
        }
    }
}
