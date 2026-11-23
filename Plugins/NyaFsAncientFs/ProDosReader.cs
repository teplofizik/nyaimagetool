using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only ProDOS 8 filesystem reader for .po images (512-byte blocks).
    /// Directory and index block pointers use ProDOS's split low/high tables.
    /// </summary>
    public sealed class ProDosReader : IFilesystemReader
    {
        private const int BlockSize = 512;
        private const int EntrySize = 39;
        private const int RootBlock = 2;

        private sealed class Node
        {
            public string Name;
            public string Path;
            public FilesystemItemType Type;
            public int StorageType;
            public uint KeyBlock;
            public uint Size;
            public uint Mode;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly Node Root;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);

        public long VolumeBytes => Data.Length;

        public ProDosReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsProDos(this.Data, null))
                throw new InvalidOperationException("Not a ProDOS filesystem");

            int Header = RootBlock * BlockSize + 4;
            Root = new Node
            {
                Name = DecodeName(Header, Data[Header] & 0x0F),
                Path = "",
                Type = FilesystemItemType.Directory,
                StorageType = 0xF,
                KeyBlock = RootBlock,
                Mode = 0x1ED
            };
            Nodes[""] = Root;
            BuildRoot();
        }

        public static bool IsProDos(byte[] Data, string SourceName)
        {
            if ((Data == null) || (Data.Length < 3 * BlockSize))
                return false;

            int Header = RootBlock * BlockSize + 4;
            int Storage = Data[Header] >> 4;
            int NameLength = Data[Header] & 0x0F;
            if ((Storage != 0xF) || (NameLength < 1) || (NameLength > 15))
                return false;

            int EntryLength = Data[Header + 31];
            int PerBlock = Data[Header + 32];
            int TotalBlocks = Data[Header + 37] | (Data[Header + 38] << 8);
            if ((EntryLength != EntrySize) || (PerBlock != 13) || (TotalBlocks == 0))
                return false;
            if ((long)TotalBlocks * BlockSize > Data.Length)
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
            var Visiting = new HashSet<uint> { RootBlock };
            ReadDirectoryBlocks(Root, RootBlock, true, Visiting);
        }

        private void ReadDirectoryBlocks(Node Parent, uint FirstBlock, bool Volume, HashSet<uint> Visiting)
        {
            uint Block = FirstBlock;
            bool IsFirst = true;
            int Guard = 0;

            while ((Block > 1) && (Block * BlockSize + BlockSize <= Data.Length) && (Guard++ < 256))
            {
                int Offset = (int)(Block * BlockSize);
                int Next = Data[Offset + 2] | (Data[Offset + 3] << 8);
                int Start = 4 + ((IsFirst && Volume) ? EntrySize : 0);
                int Count = (BlockSize - Start) / EntrySize;

                for (int I = 0; I < Count; I++)
                {
                    int Entry = Offset + Start + I * EntrySize;
                    if (Entry + EntrySize > Data.Length)
                        break;

                    int Storage = Data[Entry] >> 4;
                    int NameLength = Data[Entry] & 0x0F;
                    if ((Storage == 0) || (NameLength < 1) || (NameLength > 15))
                        continue;

                    string Name = DecodeName(Entry, NameLength);
                    if (Name.Length == 0)
                        continue;

                    uint Key = (uint)(Data[Entry + 17] | (Data[Entry + 18] << 8));
                    uint Eof = (uint)(Data[Entry + 21] | (Data[Entry + 22] << 8) | (Data[Entry + 23] << 16));

                    FilesystemItemType Type;
                    if (Storage == 0xD)
                        Type = FilesystemItemType.Directory;
                    else if ((Storage == 1) || (Storage == 2) || (Storage == 3))
                        Type = FilesystemItemType.File;
                    else
                        continue;

                    var Child = new Node
                    {
                        Name = Sanitize(Name),
                        Path = Combine(Parent.Path, Sanitize(Name)),
                        Type = Type,
                        StorageType = Storage,
                        KeyBlock = Key,
                        Size = Type == FilesystemItemType.File ? Eof : 0,
                        Mode = Type == FilesystemItemType.Directory ? 0x1EDu : 0x1A4u
                    };
                    Parent.Children.Add(Child);
                    Nodes[Child.Path] = Child;

                    if ((Type == FilesystemItemType.Directory) && Visiting.Add(Key))
                        ReadDirectoryBlocks(Child, Key, false, Visiting);
                }

                Block = (uint)Next;
                IsFirst = false;
            }
        }

        private byte[] ReadFile(Node Node)
        {
            var Result = new byte[(int)Node.Size];
            int Written = 0;

            if (Node.StorageType == 1)
            {
                CopyBlock(Node.KeyBlock, Result, ref Written);
            }
            else if (Node.StorageType == 2)
            {
                ReadIndex(Node.KeyBlock, 256, Result, ref Written);
            }
            else if (Node.StorageType == 3)
            {
                for (int I = 0; I < 128 && Written < Result.Length; I++)
                {
                    uint Index = ReadPointer(Node.KeyBlock, 128, I);
                    if (Index > 1)
                        ReadIndex(Index, 256, Result, ref Written);
                }
            }

            return Result;
        }

        private void ReadIndex(uint Block, int Count, byte[] Result, ref int Written)
        {
            for (int I = 0; I < Count && Written < Result.Length; I++)
            {
                uint DataBlock = ReadPointer(Block, Count, I);
                if (DataBlock > 1)
                    CopyBlock(DataBlock, Result, ref Written);
            }
        }

        private uint ReadPointer(uint Block, int Count, int Index)
        {
            long Offset = (long)Block * BlockSize;
            if ((Offset < 0) || (Offset + Count * 2 > Data.Length))
                return 0;
            int O = (int)Offset;
            return (uint)(Data[O + Index] | (Data[O + Count + Index] << 8));
        }

        private void CopyBlock(uint Block, byte[] Result, ref int Written)
        {
            long Offset = (long)Block * BlockSize;
            if ((Offset < 0) || (Offset + BlockSize > Data.Length) || (Written >= Result.Length))
                return;
            int Count = Math.Min(BlockSize, Result.Length - Written);
            Array.Copy(Data, Offset, Result, Written, Count);
            Written += Count;
        }

        private string DecodeName(int Offset, int Length)
        {
            if ((Length <= 0) || (Length > 15) || (Offset < 0) || (Offset + 1 + Length > Data.Length))
                return "";
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)Data[Offset + 1 + I];
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
    }
}
