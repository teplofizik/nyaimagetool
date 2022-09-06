using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Filesystem.Ext2
{
    public class Ext2FsReader : RawPacket, Universal.IFilesystemReader
    {
        private uint BlockSize;
        private uint NodesPerGroup;
        private uint INodesCount;

        private uint INodeSize => 128;

        public Ext2FsReader(byte[] Data) : base(Data) { Init(); }

        public Ext2FsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        internal Types.ExtSuperBlock SuperBlock => new Types.ExtSuperBlock(Raw, 0x400);

        /// <summary>
        /// TODO: BlockSize is 0x40 in 64bit extension...
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        internal Types.ExtBlockGroup GetBlockGroup(uint Id) => new Types.ExtBlockGroup(Raw, 0x800 + Id * 0x20);

        protected void Init()
        {
            var SB = SuperBlock;
            var BG = GetBlockGroup(0);

            BlockSize = SB.BlockSize;
            NodesPerGroup = SB.InodesPerGroup;
            INodesCount = SB.INodesCount;
        }

        private uint GetNodeSize(Types.ExtINode N)
        {
            switch(N.NodeType)
            {
                case Types.ExtINodeType.REG:
                case Types.ExtINodeType.LINK:
                    return N.SizeLo;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Read file by path
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <returns>Content of file or null if file is not exists</returns>
        public byte[] Read(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if (Node.NodeType == Types.ExtINodeType.REG)
                    return GetINodeContent(Node);
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read device information
        /// </summary>
        /// <param name="Path">Path to device</param>
        /// <returns>Device numbers (major/minor)</returns>
        public Universal.Types.DeviceInfo ReadDevice(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if ((Node.NodeType == Types.ExtINodeType.CHAR) || (Node.NodeType == Types.ExtINodeType.BLOCK))
                {
                    var Raw = Node.Block[0];
                    var Major = Raw & 0xff;
                    var Minor = (Raw >> 8) & 0xFf;
                    return new Universal.Types.DeviceInfo(Major, Minor); // TODO
                }
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read link content by path
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <returns>Link</returns>
        public string ReadLink(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if (Node.NodeType == Types.ExtINodeType.LINK)
                    return UTF8Encoding.UTF8.GetString(GetINodeContent(Node));
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read directory content
        /// </summary>
        /// <param name="Path">Path to directory</param>
        /// <returns>Array of entries</returns>
        public FilesystemEntry[] ReadDir(string Path)
        {
            var DirNode = GetINodeByPath(Path);

            if (DirNode != null)
            {
                var Res = new List<Universal.FilesystemEntry>();
                var Entries = GetDirEntries(DirNode);

                foreach(var E in Entries)
                {
                    if ((E.Name == ".") || (E.Name == "..")) continue;

                    var N = GetINode(E.INode);

                    //Console.WriteLine($"{E.Name} {E.INode}     {N}");
                    Res.Add(new Universal.FilesystemEntry(N.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), N.UID, N.GID, N.Mode, GetNodeSize(N)));
                }

                return Res.ToArray();
            }
            else
                return null;
        }

        internal Types.ExtINode GetINodeByPath(string Path)
        {
            if (Path == ".") return GetRootDir();
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            if (Path[0] == '/') Path = Path.Substring(1);

            var Parts = Path.Split("/");

            var Entries = GetDirEntries(GetRootDir());

            for (int i = 0; i < Parts.Length; i++)
            {
                var P = Parts[i];

                bool Found = false;
                foreach (var I in Entries)
                {
                    if (I.Name == P)
                    {
                        var N = GetINode(I.INode);
                        if (i == Parts.Length - 1)
                            return N;

                        if (N.NodeType == Types.ExtINodeType.DIR)
                        {
                            Entries = GetDirEntries(N);
                            Found = true;
                            break;
                        }
                        else
                            return null;
                    }
                }
                if (!Found)
                    return null;
            }

            return null;
        }

        internal Types.ExtINode GetINode(uint Id)
        {
            var BGIndex = (Id - 1) / NodesPerGroup;
            var INIndex = (Id - 1) % NodesPerGroup;

            var BG = GetBlockGroup(BGIndex);
            return new Types.ExtINode(Raw, (BG.INodeTableLo * BlockSize + INodeSize * INIndex), INodeSize);
        }

        public void Dump()
        {
            var SB = SuperBlock;
            var BG = GetBlockGroup(0);

            Console.WriteLine($"          Block size: 0x{BlockSize:x04}");
            Console.WriteLine($"         INode count: {INodesCount}");

            Console.WriteLine($"    INode table size: {INodesCount * BlockSize / 1024} kB");
            Console.WriteLine($"         Free inodes: {BG.FreeINodesCountLo}");
        }

        internal Types.ExtDirectoryEntry[] GetDirEntries(Types.ExtINode Dir)
        {
            if (Dir.NodeType != Types.ExtINodeType.DIR) throw new ArgumentException("Cannot read dir entries from non-dir INode");

            var DirContent = GetINodeContent(Dir);
            List<Types.ExtDirectoryEntry> Entries = new List<Types.ExtDirectoryEntry>();

            long Offset = 0;
            var Entry = new Types.ExtDirectoryEntry(DirContent, Offset);
            while(Entry.INode != 0)
            {
                Entries.Add(Entry);

                Offset += Entry.RecordLength;
                if (Offset == DirContent.Length)
                    break;

                Entry = new Types.ExtDirectoryEntry(DirContent, Offset);
            }

            return Entries.OrderBy(E => E.Name).ToArray();
        }

        internal byte[] GetINodeBlockContent(Types.ExtINode Node)
        {
            byte[] Res = new byte[Node.SizeLo];
            var Blocks = Node.Block;

            long DataOffset = 0;
            {
                // Direct blocks
                DataOffset = ReadToArrayFromBlockTable(Res, DataOffset, Node.SizeLo, Blocks, 12);
                // Direct data block addressing!..
                if (DataOffset == Node.SizeLo)
                    return Res;
            }

            if (Blocks[12] != 0)
            {
                // Indirect blocks 1 level
                var IndirectTable = ReadUInt32Array(Blocks[12] * BlockSize, BlockSize / 4);
                DataOffset = ReadToArrayFromBlockTable(Res, DataOffset, Node.SizeLo, IndirectTable, IndirectTable.Length);

                if (DataOffset == Node.SizeLo)
                    return Res;
            }

            if (Blocks[13] != 0)
            {
                // Indirect blocks 2 level
                var IndirectTable2 = ReadUInt32Array(Blocks[13] * BlockSize, BlockSize / 4);
                DataOffset = ReadIndirectDataTable(Res, DataOffset, Node.SizeLo, IndirectTable2, IndirectTable2.Length);

                if (DataOffset == Node.SizeLo)
                    return Res;
            }

            if(Blocks[14] != 0)
            {
                // Indirect blocks 3 level
                var IndirectTable3Offset = Blocks[14] * BlockSize;
                var IndirectTable3 = ReadUInt32Array(Blocks[14] * BlockSize, BlockSize / 4);

                foreach (var IndirectOffset2 in IndirectTable3)
                {
                    var IndirectTable2 = ReadUInt32Array(IndirectOffset2, BlockSize / 4);

                    DataOffset = ReadIndirectDataTable(Res, DataOffset, Node.SizeLo, IndirectTable2, IndirectTable2.Length);

                    if (DataOffset == Node.SizeLo)
                        return Res;
                }
            }

            if (DataOffset != Node.SizeLo) 
                Console.WriteLine($"Invalid blocks content in Node");

            return Res;
        }

        private long ReadIndirectDataTable(byte[] Data, long Offset, long Size, uint[] IndirectTable2, int TableLength)
        {
            foreach (var IndirectOffset in IndirectTable2)
            {
                if (IndirectOffset > 0)
                {
                    var IndirectTable = ReadUInt32Array(IndirectOffset * BlockSize, BlockSize / 4);

                    Offset = ReadToArrayFromBlockTable(Data, Offset, Size, IndirectTable, IndirectTable.Length);

                    if (Offset == Size)
                        return Offset;
                }
            }

            return Offset;
        }

        private long ReadToArrayFromBlockTable(byte[] Data, long Offset, long Size, uint[] Blocks, int TableLength)
        {
            for (int i = 0; i < TableLength; i++)
            {
                var B = Blocks[i];
                if (B != 0)
                {
                    var ToRead = (Size - Offset);
                    var BlockOffset = B * BlockSize;
                    var TR = (ToRead > BlockSize) ? BlockSize : ToRead;
                    var ReadOutData = ReadArray(BlockOffset, TR);

                    Data.WriteArray(Offset, ReadOutData, TR);

                    Offset += TR;
                    if (Offset == Size)
                        return Offset;
                }
                else
                {
                    Offset += BlockSize;

                    if (Offset == Size)
                        return Offset;
                }
            }

            return Offset;
        }

        internal byte[] GetINodeContent(Types.ExtINode Node)
        {
            var Size = Node.SizeLo;
            var Blocks = Node.Block;
            var Type = Node.NodeType;

            if (Size == 0)
                return new byte[] { };

            // Тип ноды
            switch (Type)
            {
                case Types.ExtINodeType.LINK:
                    if (Size > 60)
                        return GetINodeBlockContent(Node);
                    else
                        return Node.BlockRaw;

                default:
                    return GetINodeBlockContent(Node);
            }
        }

        internal Types.ExtINode GetRootDir()
        {
            for (uint i = 1; i < INodesCount; i++)
            {
                var N = GetINode(i);

                if (N.NodeType == Types.ExtINodeType.DIR)
                    return N;
            }

            return null;
        }
    }
}
