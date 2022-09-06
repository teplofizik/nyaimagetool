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
        uint INodeIndex = 1;
        private List<BuilderDirectory> Dirs = new List<BuilderDirectory>();

        public Ext2FsBuilder(int DiskSize) : base(new byte[DiskSize])
        {
            Fill(0x00);

            InitSuperblock();
        }

        public void InitSuperblock()
        {
            Superblock.LogBlockSize = 0; // BlockSize => 0x400
            Superblock.LogClusterSize = 0;
            Superblock.BlocksCount = Convert.ToUInt32(getLength() / Superblock.BlockSize);
            Superblock.BlocksPerGroup = 0x2000; // TODO: Calc
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

        /// <summary>
        /// Get builded filesystem image
        /// </summary>
        /// <returns></returns>
        public byte[] GetFilesystemImage()
        {
            foreach(var D in Dirs)
            {
                SetNodeBlockContent(D.Node, D.Content);
            }
            return getPacket();
        }

        private BuilderDirectory GetParentDirectory(string Path)
        {
            var Parent = Universal.Helper.FsHelper.GetParentDirPath(Path);

            foreach(var D in Dirs)
            {
                if(D.Path == Path)
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
            INode.Mode = Mode;
            INode.CTime = Universal.Helper.FsHelper.ConvertToUnixTimestamp(DateTime.Now);
            INode.MTime = INode.CTime;
            INode.ATime = INode.CTime;
            INode.DTime = 0;
            INode.BlocksLo = 0;
            INode.Block.Fill(0u);
            INode.LinksCount = 1;

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

                Dirs.Add(new BuilderDirectory(Path, N));
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
                var N = CreateNewINode(Types.ExtINodeType.SOCK, User, Group, Mode);
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

        void SetNodeBlockContent(Types.ExtINode Node, byte[] Content)
        {

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
                    var Res = new List<byte>();
                    foreach(var E in Entries)
                        Res.AddRange(E.getPacket());

                    return Res.ToArray();
                }
            }
        }
    }
}
