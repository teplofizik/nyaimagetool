using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only Amiga OFS/FFS filesystem reader for 880K ADF images.
    /// </summary>
    public sealed class AdfReader : IFilesystemReader
    {
        private const int BlockSize = 512;
        private const int MaxDataBlocks = 72;
        private const int RootType = 2;
        private const int DirType = 2;
        private const int FileType = -3;
        private const int OfsDataSize = 488;

        private sealed class Node
        {
            public string Name;
            public string Path;
            public FilesystemItemType Type;
            public uint Block;
            public uint Size;
            public uint Mode;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly bool Ffs;
        private readonly uint RootBlock;
        private readonly Node Root;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);

        public long VolumeBytes => Data.Length;

        public AdfReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsAdf(this.Data))
                throw new InvalidOperationException("Not an Amiga ADF filesystem");

            Ffs = (this.Data[3] & 1) != 0;
            RootBlock = ReadBe32(8);
            if ((RootBlock == 0) || (RootBlock * BlockSize >= this.Data.Length))
                RootBlock = 880;

            Root = new Node
            {
                Name = DecodeName((int)(RootBlock * BlockSize + 433), this.Data[(int)(RootBlock * BlockSize + 432)]),
                Path = "",
                Type = FilesystemItemType.Directory,
                Block = RootBlock,
                Mode = 0x1ED
            };
            Nodes[""] = Root;
            BuildRoot();
        }

        public static bool IsAdf(byte[] Data)
        {
            if ((Data == null) || (Data.Length < 901120))
                return false;
            if ((Data[0] != (byte)'D') || (Data[1] != (byte)'O') || (Data[2] != (byte)'S'))
                return false;
            if (Data[3] > 3)
                return false;

            uint Root = ReadBe32(Data, 8);
            if ((Root == 0) || (Root * (long)BlockSize + 512 > Data.Length))
                Root = 880;
            if ((Root * (long)BlockSize + 512 > Data.Length) || (ReadBe32(Data, (int)(Root * BlockSize)) != RootType))
                return false;
            return true;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if ((Node == null) || (Node.Type != FilesystemItemType.File))
                return null;
            return ReadFile(Node);
        }

        public string ReadLink(string Path) => null;

        public DeviceInfo ReadDevice(string Path) => null;

        public FilesystemEntry[] ReadDir(string Path)
        {
            var Node = Find(Path);
            if ((Node == null) || (Node.Type != FilesystemItemType.Directory))
                return Array.Empty<FilesystemEntry>();

            var Parent = Normalize(Path);
            var Result = new List<FilesystemEntry>();
            foreach (var Child in Node.Children)
            {
                Result.Add(new FilesystemEntry(
                    Child.Type,
                    Combine(Parent, Child.Name),
                    0,
                    0,
                    Child.Mode,
                    Child.Type == FilesystemItemType.Directory ? 0u : Child.Size));
            }
            return Result.ToArray();
        }

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode)
        {
            User = 0;
            Group = 0;
            Mode = Root.Mode;
            return true;
        }

        private void BuildRoot()
        {
            int RootOffset = (int)(RootBlock * BlockSize);
            int HashSize = (int)ReadBe32(RootOffset + 12);
            if ((HashSize <= 0) || (HashSize > 1024))
                HashSize = 72;

            var Visiting = new HashSet<uint>();
            for (int I = 0; I < HashSize; I++)
            {
                uint Block = ReadBe32(RootOffset + 24 + I * 4);
                AddHashChain(Root, Block, Visiting);
            }
        }

        private void AddHashChain(Node Parent, uint Block, HashSet<uint> Visiting)
        {
            int Guard = 0;
            while ((Block > 1) && (Block * BlockSize + BlockSize <= Data.Length) && (Guard++ < 4096))
            {
                var Child = CreateNode(Parent, Block);
                if (Child != null)
                {
                    Parent.Children.Add(Child);
                    Nodes[Child.Path] = Child;
                    if (Child.Type == FilesystemItemType.Directory)
                        BuildDirectory(Child, Visiting);
                }

                int Offset = (int)(Block * BlockSize);
                Block = ReadBe32(Offset + 496);
            }
        }

        private void BuildDirectory(Node Parent, HashSet<uint> Visiting)
        {
            if (!Visiting.Add(Parent.Block))
                return;

            int Offset = (int)(Parent.Block * BlockSize);
            for (int I = 0; I < MaxDataBlocks; I++)
            {
                uint Block = ReadBe32(Offset + 24 + (MaxDataBlocks - 1 - I) * 4);
                if ((Block <= 1) || (Block * BlockSize + BlockSize > Data.Length))
                    continue;

                var Child = CreateNode(Parent, Block);
                if (Child == null)
                    continue;

                Parent.Children.Add(Child);
                Nodes[Child.Path] = Child;
                if (Child.Type == FilesystemItemType.Directory)
                    BuildDirectory(Child, Visiting);
            }
        }

        private Node CreateNode(Node Parent, uint Block)
        {
            int Offset = (int)(Block * BlockSize);
            if (ReadBe32(Offset) != RootType)
                return null;

            byte NameLength = Data[Offset + 432];
            string Name = DecodeName(Offset + 433, NameLength);
            if (Name.Length == 0)
                return null;

            int SecType = (int)ReadBe32(Offset + 508);
            FilesystemItemType Type;
            if (SecType == 2)
                Type = FilesystemItemType.Directory;
            else if (SecType == -3)
                Type = FilesystemItemType.File;
            else
                return null;

            uint Access = ReadBe32(Offset + 320);
            uint Mode = AccessToMode(Access, Type == FilesystemItemType.Directory);
            uint Size = ReadBe32(Offset + 324);

            return new Node
            {
                Name = Sanitize(Name),
                Path = Combine(Parent.Path, Sanitize(Name)),
                Type = Type,
                Block = Block,
                Size = Type == FilesystemItemType.Directory ? 0 : Size,
                Mode = Mode
            };
        }

        private byte[] ReadFile(Node Node)
        {
            var Result = new byte[Node.Size];
            if (Ffs)
                ReadFfs(Node, Result);
            else
                ReadOfs(Node, Result);
            return Result;
        }

        private void ReadFfs(Node Node, byte[] Result)
        {
            int HeaderOffset = (int)(Node.Block * BlockSize);
            int Written = 0;
            uint Extension = ReadBe32(HeaderOffset + 504);

            for (int I = 0; I < MaxDataBlocks && Written < Result.Length; I++)
            {
                uint Block = (I == 0)
                    ? ReadBe32(HeaderOffset + 16)
                    : ReadBe32(HeaderOffset + 24 + (MaxDataBlocks - 1 - I) * 4);
                if (Block <= 1)
                    continue;
                CopyBlock(Block, Result, ref Written);
            }

            int Guard = 0;
            while ((Extension > 1) && (Written < Result.Length) && (Guard++ < 256))
            {
                int ExtOffset = (int)(Extension * BlockSize);
                if ((ExtOffset + BlockSize > Data.Length) || (ReadBe32(ExtOffset) != 16))
                    break;

                for (int I = 0; I < MaxDataBlocks && Written < Result.Length; I++)
                {
                    uint Block = ReadBe32(ExtOffset + 24 + (MaxDataBlocks - 1 - I) * 4);
                    if (Block > 1)
                        CopyBlock(Block, Result, ref Written);
                }
                Extension = ReadBe32(ExtOffset + 504);
            }
        }

        private void ReadOfs(Node Node, byte[] Result)
        {
            int HeaderOffset = (int)(Node.Block * BlockSize);
            uint Block = ReadBe32(HeaderOffset + 16);
            int Written = 0;
            int Guard = 0;

            while ((Block > 1) && (Written < Result.Length) && (Guard++ < 4096))
            {
                int Offset = (int)(Block * BlockSize);
                if (Offset + BlockSize > Data.Length)
                    break;

                uint DataSize = ReadBe32(Offset + 12);
                int Count = (int)Math.Min(DataSize, (uint)OfsDataSize);
                Count = (int)Math.Min(Count, Result.Length - Written);
                if (Count > 0)
                    Array.Copy(Data, Offset + 24, Result, Written, Count);
                Written += Count;

                Block = ReadBe32(Offset + 16);
            }
        }

        private void CopyBlock(uint Block, byte[] Result, ref int Written)
        {
            long Offset = (long)Block * BlockSize;
            if ((Offset < 0) || (Offset + BlockSize > Data.Length))
                return;
            int Count = Math.Min(BlockSize, Result.Length - Written);
            Array.Copy(Data, Offset, Result, Written, Count);
            Written += Count;
        }

        private static uint AccessToMode(uint Access, bool Directory)
        {
            uint Mode = Directory ? 0x1EDu : 0x1A4u;
            if ((Access & 4) == 0)
                Mode &= ~0x92u;
            return Mode;
        }

        private string DecodeName(int Offset, int Length)
        {
            if ((Length <= 0) || (Length > 30) || (Offset < 0) || (Offset + Length > Data.Length))
                return "";
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)Data[Offset + I];
            return new string(Chars);
        }

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
        }

        private static string Sanitize(string Name) =>
            Name.Replace('/', '_').Replace('\\', '_');

        private static string Normalize(string Path)
        {
            if (String.IsNullOrEmpty(Path) || (Path == ".") || (Path == "/"))
                return "";
            return Path.Replace('\\', '/').Trim('/');
        }

        private static string Combine(string Parent, string Name) =>
            String.IsNullOrEmpty(Parent) ? Name : Parent + "/" + Name;

        private uint ReadBe32(int Offset)
        {
            if ((Offset < 0) || (Offset + 4 > Data.Length))
                return 0;
            return ((uint)Data[Offset] << 24) | ((uint)Data[Offset + 1] << 16) |
                ((uint)Data[Offset + 2] << 8) | Data[Offset + 3];
        }

        private static uint ReadBe32(byte[] Data, int Offset)
        {
            if ((Offset < 0) || (Offset + 4 > Data.Length))
                return 0;
            return ((uint)Data[Offset] << 24) | ((uint)Data[Offset + 1] << 16) |
                ((uint)Data[Offset + 2] << 8) | Data[Offset + 3];
        }
    }
}
