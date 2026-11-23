using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only Macintosh File System (MFS, 1984) reader. MFS is a flat
    /// namespace: all files live in the root and Finder folders are only
    /// metadata. Resource forks are not exposed.
    /// </summary>
    public sealed class MfsReader : IFilesystemReader
    {
        private const int MdbBlock = 2;
        private const int BlockSize = 512;
        private const ushort Signature = 0xD2D7;
        private const int MdbSize = 64;
        private const int DirectoryRecordFixedSize = 51;

        private sealed class Node
        {
            public string Name;
            public string Path;
            public uint Size;
            public uint Mode;
            public ushort FirstBlock;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly int ContainerBlocks;
        private readonly ushort DirectoryStartBlock;
        private readonly ushort DirectoryBlockCount;
        private readonly ushort AllocationBlocksStartBlock;
        private readonly ushort AllocationBlockCount;
        private readonly uint AllocationBlockSize;
        private readonly byte[] Vabm;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly Node Root;

        public long VolumeBytes => Data.Length;

        public MfsReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsMfs(this.Data))
                throw new InvalidOperationException("Not an MFS filesystem");

            ContainerBlocks = Data.Length / BlockSize;
            DirectoryStartBlock = ReadBe16(MdbBlock * BlockSize + 14);
            DirectoryBlockCount = ReadBe16(MdbBlock * BlockSize + 16);
            AllocationBlockCount = ReadBe16(MdbBlock * BlockSize + 18);
            AllocationBlockSize = ReadBe32(MdbBlock * BlockSize + 20);
            AllocationBlocksStartBlock = ReadBe16(MdbBlock * BlockSize + 28);

            int VabmSize = (AllocationBlockCount + 1) / 2 * 3;
            Vabm = new byte[VabmSize];
            Array.Copy(Data, MdbBlock * BlockSize + MdbSize, Vabm, 0, VabmSize);

            Root = new Node
            {
                Name = "",
                Path = "",
                Mode = 0x1ED
            };
            Nodes[""] = Root;
            BuildTree();
        }

        public static bool IsMfs(byte[] Data)
        {
            if ((Data == null) || (Data.Length < (MdbBlock + 1) * BlockSize) ||
                ((Data.Length % BlockSize) != 0))
                return false;

            int Mdb = MdbBlock * BlockSize;
            if (ReadBe16(Data, Mdb) != Signature)
                return false;

            int ContainerBlocks = Data.Length / BlockSize;
            int DirectoryStart = ReadBe16(Data, Mdb + 14);
            int DirectoryCount = ReadBe16(Data, Mdb + 16);
            int AllocationCount = ReadBe16(Data, Mdb + 18);
            uint AllocationSize = ReadBe32(Data, Mdb + 20);
            int AllocationStart = ReadBe16(Data, Mdb + 28);

            if ((DirectoryCount == 0) || (DirectoryStart < 3) ||
                (DirectoryStart >= ContainerBlocks) ||
                (DirectoryStart + DirectoryCount >= ContainerBlocks))
                return false;
            if ((AllocationCount == 0) || (AllocationStart >= ContainerBlocks))
                return false;
            if ((AllocationSize < BlockSize) || (AllocationSize > 65536) ||
                ((AllocationSize & (AllocationSize - 1)) != 0))
                return false;

            int VabmSize = (AllocationCount + 1) / 2 * 3;
            if (Mdb + MdbSize + VabmSize > Data.Length)
                return false;

            return true;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if (Node == null)
                return null;

            var Result = new byte[Node.Size];
            int Block = Node.FirstBlock;
            long Written = 0;
            int Guard = 0;

            while ((Block > 1) && (Written < Result.Length) && (Guard++ < AllocationBlockCount + 2))
            {
                long Offset = (long)AllocationBlocksStartBlock * BlockSize +
                    (long)(Block - 2) * AllocationBlockSize;
                if ((Offset < 0) || (Offset >= Data.Length))
                    break;

                int Count = (int)Math.Min(AllocationBlockSize, (long)Result.Length - Written);
                Count = (int)Math.Min(Count, Data.Length - Offset);
                if (Count <= 0)
                    break;

                Array.Copy(Data, Offset, Result, Written, Count);
                Written += Count;
                Block = VabmNext(Block);
            }

            return Result;
        }

        public string ReadLink(string Path) => null;

        public DeviceInfo ReadDevice(string Path) => null;

        public FilesystemEntry[] ReadDir(string Path)
        {
            var Node = Find(Path);
            if (Node == null)
                return Array.Empty<FilesystemEntry>();

            var Parent = Normalize(Path);
            var Result = new List<FilesystemEntry>();
            foreach (var Child in Node.Children)
            {
                Result.Add(new FilesystemEntry(
                    FilesystemItemType.File,
                    Combine(Parent, Child.Name),
                    0,
                    0,
                    Child.Mode,
                    Child.Size));
            }
            return Result.ToArray();
        }

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode)
        {
            User = 0;
            Group = 0;
            Mode = 0x1ED;
            return true;
        }

        private void BuildTree()
        {
            for (int BlockIndex = 0; BlockIndex < DirectoryBlockCount; BlockIndex++)
            {
                int BlockOffset = (DirectoryStartBlock + BlockIndex) * BlockSize;
                int Offset = 0;

                while (Offset + DirectoryRecordFixedSize <= BlockSize)
                {
                    int Attributes = Data[BlockOffset + Offset];
                    if ((Attributes & 0x80) == 0)
                        break;

                    int NameLength = Data[BlockOffset + Offset + 50];
                    if ((NameLength == 0) || (Offset + DirectoryRecordFixedSize + NameLength > BlockSize))
                        break;

                    int FileNumber = (int)ReadBe32(BlockOffset + Offset + 18);
                    ushort FirstBlock = ReadBe16(BlockOffset + Offset + 22);
                    uint Length = ReadBe32(BlockOffset + Offset + 24);
                    string Name = DecodeName(BlockOffset + Offset + 51, NameLength);
                    if (Name.Length == 0)
                        break;

                    uint Mode = ((Attributes & 0x01) != 0) ? 0x124u : 0x1A4u;
                    var Node = new Node
                    {
                        Name = Sanitize(Name),
                        Path = "",
                        Size = Length,
                        Mode = Mode,
                        FirstBlock = FirstBlock
                    };

                    if (Node.Name.Length > 0)
                    {
                        Node.Path = Node.Name;
                        Nodes[Node.Path] = Node;
                        Root.Children.Add(Node);
                    }

                    int RecordSize = DirectoryRecordFixedSize + NameLength;
                    if ((RecordSize & 1) != 0)
                        RecordSize++;
                    Offset += RecordSize;
                }
            }
        }

        private ushort VabmNext(int AllocationBlock)
        {
            int Index = AllocationBlock - 2;
            if (Index < 0)
                return 1;

            int ByteIndex = Index * 3 / 2;
            if (ByteIndex + 1 >= Vabm.Length)
                return 1;

            if ((Index & 1) == 0)
                return (ushort)((Vabm[ByteIndex] << 4) | ((Vabm[ByteIndex + 1] >> 4) & 0x0F));
            else
                return (ushort)(((Vabm[ByteIndex] & 0x0F) << 8) | Vabm[ByteIndex + 1]);
        }

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
        }

        private string DecodeName(int Offset, int Length)
        {
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)Data[Offset + I];
            return new string(Chars).TrimEnd('\0');
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

        private ushort ReadBe16(long Offset)
        {
            int O = checked((int)Offset);
            return (ushort)((Data[O] << 8) | Data[O + 1]);
        }

        private uint ReadBe32(long Offset)
        {
            int O = checked((int)Offset);
            return ((uint)Data[O] << 24) | ((uint)Data[O + 1] << 16) |
                ((uint)Data[O + 2] << 8) | Data[O + 3];
        }

        private static ushort ReadBe16(byte[] Data, long Offset)
        {
            int O = checked((int)Offset);
            return (ushort)((Data[O] << 8) | Data[O + 1]);
        }

        private static uint ReadBe32(byte[] Data, long Offset)
        {
            int O = checked((int)Offset);
            return ((uint)Data[O] << 24) | ((uint)Data[O + 1] << 16) |
                ((uint)Data[O + 2] << 8) | Data[O + 3];
        }
    }
}
