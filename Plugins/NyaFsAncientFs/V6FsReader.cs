using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only UNIX V6 (PDP-11) filesystem reader. The on-disk format has a
    /// 512-byte superblock at block 1, 32-byte inodes from block 2, and 16-byte
    /// directory entries. Large files use the ILARG single-indirect format.
    /// </summary>
    public sealed class V6FsReader : IFilesystemReader
    {
        private const int BlockSize = 512;
        private const int InodesPerBlock = 16;
        private const int InodeSize = 32;
        private const int RootInodeNumber = 1;
        private const ushort IAlloc = 0x8000;
        private const ushort ILarg = 0x1000;

        private sealed class Inode
        {
            public ushort Mode;
            public byte Uid;
            public byte Gid;
            public uint Size;
            public ushort[] Addresses;
        }

        private sealed class Node
        {
            public string Name;
            public string Path;
            public FilesystemItemType Type;
            public Inode Inode;
            public uint Device;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly int InodeBlocks;
        private readonly int FilesystemBlocks;
        private readonly Dictionary<uint, Inode> Inodes = new Dictionary<uint, Inode>();
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly Node Root;

        public long VolumeBytes => Data.Length;

        public V6FsReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsV6(this.Data))
                throw new InvalidOperationException("Not a UNIX V6 filesystem");

            InodeBlocks = ReadU16(BlockSize);
            FilesystemBlocks = ReadU16(BlockSize + 2);

            var RootInode = ReadInode(RootInodeNumber);
            Root = new Node
            {
                Name = "",
                Path = "",
                Type = FilesystemItemType.Directory,
                Inode = RootInode
            };
            Nodes[""] = Root;
            Inodes[RootInodeNumber] = RootInode;
            Build(Root, new HashSet<uint> { RootInodeNumber });
        }

        public static bool IsV6(byte[] Data)
        {
            if ((Data == null) || (Data.Length < BlockSize * 3))
                return false;

            int InodeBlocks = ReadU16(Data, BlockSize);
            int Blocks = ReadU16(Data, BlockSize + 2);
            if ((InodeBlocks <= 0) || (InodeBlocks > 128) ||
                (Blocks <= InodeBlocks + 2) || (Blocks * BlockSize > Data.Length))
                return false;

            var Root = ReadInode(Data, RootInodeNumber, InodeBlocks);
            if (Root == null)
                return false;
            if ((Root.Mode & IAlloc) == 0)
                return false;
            if ((Root.Mode & 0x4000) == 0)
                return false;
            if ((Root.Addresses[0] == 0) || (Root.Addresses[0] >= Blocks))
                return false;
            if ((Root.Size == 0) || (Root.Size > 65536))
                return false;

            return true;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if ((Node == null) || (Node.Type != FilesystemItemType.File))
                return null;

            return ReadFile(Node.Inode);
        }

        public string ReadLink(string Path) => null;

        public DeviceInfo ReadDevice(string Path)
        {
            var Node = Find(Path);
            if ((Node == null) || ((Node.Type != FilesystemItemType.Character) && (Node.Type != FilesystemItemType.Block)))
                return null;

            return new DeviceInfo((uint)(Node.Device >> 8), Node.Device & 0xFF);
        }

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
                    Child.Inode.Uid,
                    Child.Inode.Gid,
                    (uint)(Child.Inode.Mode & 0x1FF),
                    (Child.Type == FilesystemItemType.Directory) ? 0u : Child.Inode.Size));
            }
            return Result.ToArray();
        }

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode)
        {
            User = Root.Inode.Uid;
            Group = Root.Inode.Gid;
            Mode = (uint)(Root.Inode.Mode & 0x1FF);
            return true;
        }

        private void Build(Node Parent, HashSet<uint> Visiting)
        {
            var Data = ReadFile(Parent.Inode);
            for (int Offset = 0; Offset + 16 <= Data.Length; Offset += 16)
            {
                uint ChildNumber = (uint)(Data[Offset] | (Data[Offset + 1] << 8));
                if (ChildNumber == 0)
                    continue;

                string Name = DecodeName(Data, Offset + 2, 14);
                if ((Name.Length == 0) || (Name == ".") || (Name == ".."))
                    continue;

                var ChildInode = ReadInode(ChildNumber);
                if (ChildInode == null)
                    continue;

                var Type = TypeOf(ChildInode.Mode);
                var Child = new Node
                {
                    Name = Name,
                    Path = Combine(Parent.Path, Name),
                    Type = Type,
                    Inode = ChildInode,
                    Device = ChildInode.Addresses[0]
                };
                Parent.Children.Add(Child);
                Nodes[Child.Path] = Child;

                if ((Type == FilesystemItemType.Directory) && Visiting.Add(ChildNumber))
                    Build(Child, Visiting);
            }
        }

        private byte[] ReadFile(Inode Inode)
        {
            var Result = new byte[(int)Inode.Size];
            int BlockCount = (int)((Inode.Size + BlockSize - 1) / BlockSize);
            for (int Block = 0; Block < BlockCount; Block++)
            {
                int Physical = MapBlock(Inode, Block);
                if (Physical == 0)
                    continue;

                long Source = (long)Physical * BlockSize;
                if ((Source < 0) || (Source >= Data.Length))
                    continue;

                int Offset = Block * BlockSize;
                int Count = (int)Math.Min(BlockSize, (long)Result.Length - Offset);
                Count = (int)Math.Min(Count, Data.Length - Source);
                if (Count > 0)
                    Array.Copy(Data, Source, Result, Offset, Count);
            }
            return Result;
        }

        private int MapBlock(Inode Inode, int Block)
        {
            if ((Inode.Mode & ILarg) == 0)
                return (Block < 8) ? Inode.Addresses[Block] : 0;

            if ((Block < 0) || (Block >= 128) || (Inode.Addresses[0] == 0))
                return 0;

            long Indirect = (long)Inode.Addresses[0] * BlockSize + Block * 2;
            if ((Indirect < 0) || (Indirect + 2 > Data.Length))
                return 0;

            return Data[Indirect] | (Data[Indirect + 1] << 8);
        }

        private Inode ReadInode(uint Number)
        {
            if ((Number == 0) || (Number > InodeBlocks * InodesPerBlock))
                return null;

            long Offset = BlockSize * 2L + (Number - 1) * InodeSize;
            if ((Offset < 0) || (Offset + InodeSize > Data.Length))
                return null;

            return ReadInode(Data, Offset);
        }

        private static Inode ReadInode(byte[] Data, uint Number, int InodeBlocks)
        {
            if ((Number == 0) || (Number > InodeBlocks * InodesPerBlock))
                return null;
            long Offset = BlockSize * 2L + (Number - 1) * InodeSize;
            if ((Offset < 0) || (Offset + InodeSize > Data.Length))
                return null;
            return ReadInode(Data, Offset);
        }

        private static Inode ReadInode(byte[] Data, long Offset)
        {
            int O = (int)Offset;
            var Result = new Inode
            {
                Mode = (ushort)(Data[O] | (Data[O + 1] << 8)),
                Uid = Data[O + 3],
                Gid = Data[O + 4],
                Size = (uint)((Data[O + 5] << 16) | Data[O + 6] | (Data[O + 7] << 8)),
                Addresses = new ushort[8]
            };

            for (int I = 0; I < 8; I++)
                Result.Addresses[I] = (ushort)(Data[O + 8 + I * 2] | (Data[O + 9 + I * 2] << 8));

            return Result;
        }

        private FilesystemItemType TypeOf(ushort Mode)
        {
            if ((Mode & 0x6000) == 0x6000)
                return FilesystemItemType.Block;
            if ((Mode & 0x4000) != 0)
                return FilesystemItemType.Directory;
            if ((Mode & 0x2000) != 0)
                return FilesystemItemType.Character;
            return FilesystemItemType.File;
        }

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
        }

        private static string DecodeName(byte[] Data, int Offset, int Length)
        {
            int End = Offset;
            while ((End < Offset + Length) && (Data[End] != 0))
                End++;
            var Chars = new char[End - Offset];
            for (int I = 0; I < Chars.Length; I++)
                Chars[I] = (char)Data[Offset + I];
            return new string(Chars);
        }

        private static string Normalize(string Path)
        {
            if (String.IsNullOrEmpty(Path) || (Path == ".") || (Path == "/"))
                return "";
            return Path.Replace('\\', '/').Trim('/');
        }

        private static string Combine(string Parent, string Name) =>
            String.IsNullOrEmpty(Parent) ? Name : Parent + "/" + Name;

        private ushort ReadU16(long Offset)
        {
            int O = (int)Offset;
            return (ushort)(Data[O] | (Data[O + 1] << 8));
        }

        private static ushort ReadU16(byte[] Data, int Offset) =>
            (ushort)(Data[Offset] | (Data[Offset + 1] << 8));
    }
}
