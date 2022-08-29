using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Implementations
{
    public class Ext2Fs : ExtFs
    {
        private uint BlockSize;
        private uint NodesPerGroup;
        private uint INodesCount;

        private uint INodeSize => 128;

        public Ext2Fs(byte[] Data) : base(Data) { }

        public Ext2Fs(string Filename) : base(Filename) { }

        protected override void Init()
        {
            base.Init();

            var SB = SuperBlock;
            var BG = GetBlockGroup(0);

            BlockSize = SB.BlockSize;
            NodesPerGroup = SB.InodesPerGroup;
            INodesCount = SB.INodesCount;
        }

        private string CombinePath(string Base, string Name)
        {
            if ((Base == "/") || (Base == ".")) return Name;

            return Base + "/" + Name;
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

        public Types.FilesystemEntry[] ReadDir(string Path)
        {
            var DirNode = GetINodeByPath(Path);

            if (DirNode != null)
            {
                var Res = new List<Types.FilesystemEntry>();
                var Entries = GetDirEntries(DirNode);

                foreach(var E in Entries)
                {
                    if ((E.Name == ".") || (E.Name == "..")) continue;

                    var N = GetINode(E.INode);

                    Console.WriteLine($"{E.Name} {E.INode}     {N}");
                    Res.Add(new Types.FilesystemEntry(N.FsNodeType, CombinePath(Path, E.Name), N.UID, N.GID, N.Mode, GetNodeSize(N)));
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

        public override void Dump()
        {
            var SB = SuperBlock;
            var BG = GetBlockGroup(0);

            Console.WriteLine($"          Block size: 0x{BlockSize:x04}");
            Console.WriteLine($"         INode count: {INodesCount}");

            Console.WriteLine($"    INode table size: {INodesCount * BlockSize / 1024} kB");
            Console.WriteLine($"         Free inodes: {BG.FreeINodesCountLo}");

            var RootDir = ReadDir(".");
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

            return Entries.ToArray();
        }

        internal byte[] GetINodeBlockContent(Types.ExtINode Node)
        {
            byte[] Res = new byte[Node.SizeLo];
            var Blocks = Node.Block;

            long ToRead = Node.SizeLo;
            long DataOffset = 0;
            // Direct data block addressing!..
            for(int i = 0; i < 12; i++)
            {
                var B = Blocks[i];
                if (B != 0)
                {
                    var BlockOffset = B * BlockSize;
                    var TR = (ToRead > BlockSize) ? BlockSize : ToRead;
                    var ReadOutData = ReadArray(BlockOffset, TR);

                    Res.WriteArray(DataOffset, ReadOutData, TR);

                    DataOffset += TR;
                    ToRead -= TR;
                    if (ToRead == 0)
                        return Res;
                }
                else
                    throw new InvalidOperationException("Invalid INode block data...");
            }

            throw new NotImplementedException("Indirect INode data blocka are not implemented now!");
        }

        internal byte[] GetINodeContent(Types.ExtINode Node)
        {
            var Size = Node.SizeLo;
            var Blocks = Node.Block;
            var Type = Node.NodeType;

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

        public override void DumpINodes()
        {
            Console.WriteLine($"INode table:");
            for (uint i = 1; i < INodesCount; i++)
            {
                var N = GetINode(i);

                if(N.NodeType != Types.ExtINodeType.NONE)
                    Console.WriteLine($"{i:0000}: t:{N.NodeType} m:{N.ModeStr:x04} u:{N.UID} g:{N.GID} s:{N.SizeLo} l:{N.LinksCount} f:{N.Flags}");
            }
        }
    }
}
