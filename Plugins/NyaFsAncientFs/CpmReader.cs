using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaFsAncientFs
{
    /// <summary>
    /// CP/M disk geometry. CP/M does not store this in the filesystem, so the
    /// plugin carries a small table of common formats and can also be given an
    /// explicit geometry.
    /// </summary>
    public sealed class CpmGeometry
    {
        public readonly string Name;
        public readonly int SectorSize;
        public readonly int Tracks;
        public readonly int SectorsPerTrack;
        public readonly int BlockSize;
        public readonly int DirectoryEntries;
        public readonly int Skew;
        public readonly int ReservedTracks;
        public readonly int[] SkewTable;

        public CpmGeometry(string Name, int SectorSize, int Tracks, int SectorsPerTrack,
            int BlockSize, int DirectoryEntries, int Skew, int ReservedTracks, int[] SkewTable = null)
        {
            this.Name = Name;
            this.SectorSize = SectorSize;
            this.Tracks = Tracks;
            this.SectorsPerTrack = SectorsPerTrack;
            this.BlockSize = BlockSize;
            this.DirectoryEntries = DirectoryEntries;
            this.Skew = Skew;
            this.ReservedTracks = ReservedTracks;
            this.SkewTable = SkewTable;
        }

        public long TotalBytes => (long)SectorSize * SectorsPerTrack * Tracks;
        public int BlockCount => (int)((TotalBytes - (long)ReservedTracks * SectorSize * SectorsPerTrack) / BlockSize);
    }

    /// <summary>
    /// Read-only CP/M 2.2/CP/M 3 filesystem reader. CP/M has a flat namespace;
    /// user areas above zero are exposed as uNN directories.
    /// </summary>
    public sealed class CpmReader : IFilesystemReader
    {
        private sealed class DirEntry
        {
            public int User;
            public string Name;
            public int Extent;
            public int Records;
            public int BytesInLastRecord;
            public bool ReadOnly;
            public int[] Blocks;
        }

        private sealed class FileExtent
        {
            public DirEntry Entry;
            public int LocalExtent;
            public int Records;
            public int BytesInLastRecord;
        }

        private sealed class Node
        {
            public string Name;
            public string Path;
            public FilesystemItemType Type;
            public uint Size;
            public uint Mode;
            public int User;
            public Dictionary<int, FileExtent> Extents;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly int BlockCount;
        private readonly int DirectoryBlocks;
        private readonly int PointersPerEntry;
        private readonly int ExtentsPerPhysical;
        private readonly int[] SkewTable;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly Node Root;

        public readonly CpmGeometry Geometry;

        public long VolumeBytes => Data.Length;

        public CpmReader(byte[] Data)
        {
            if (!TryDetect(Data, null, out var Geometry, false))
                throw new InvalidOperationException("Not a CP/M filesystem");

            this.Data = Data;
            this.Geometry = Geometry;
            BlockCount = Geometry.BlockCount;
            PointersPerEntry = (BlockCount < 256) ? 16 : 8;
            ExtentsPerPhysical = Math.Max(1, PointersPerEntry * Geometry.BlockSize / 16384);
            SkewTable = BuildSkewTable(Geometry);
            DirectoryBlocks = (Geometry.DirectoryEntries * 32 + Geometry.BlockSize - 1) / Geometry.BlockSize;

            Root = new Node
            {
                Name = "",
                Path = "",
                Type = FilesystemItemType.Directory,
                Mode = 0x1ED
            };
            Nodes[""] = Root;
            BuildTree();
        }

        public CpmReader(byte[] Data, CpmGeometry Geometry)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            this.Geometry = Geometry ?? throw new ArgumentNullException(nameof(Geometry));

            if (Data.Length != Geometry.TotalBytes)
                throw new InvalidOperationException("CP/M image size does not match the geometry");
            if ((Geometry.Tracks <= 0) || (Geometry.SectorsPerTrack <= 0) ||
                (Geometry.ReservedTracks < 0) || (Geometry.ReservedTracks >= Geometry.Tracks) ||
                (Geometry.DirectoryEntries <= 0) ||
                (Geometry.SectorSize < 128) || (Geometry.SectorSize % 128 != 0) ||
                (Geometry.BlockSize < 1024) || (Geometry.BlockSize % Geometry.SectorSize != 0) ||
                ((Geometry.BlockSize & (Geometry.BlockSize - 1)) != 0))
                throw new InvalidOperationException("Invalid CP/M geometry");

            BlockCount = Geometry.BlockCount;
            if (BlockCount <= 0)
                throw new InvalidOperationException("Invalid CP/M block count");

            PointersPerEntry = (BlockCount < 256) ? 16 : 8;
            ExtentsPerPhysical = Math.Max(1, PointersPerEntry * Geometry.BlockSize / 16384);
            SkewTable = BuildSkewTable(Geometry);
            DirectoryBlocks = (Geometry.DirectoryEntries * 32 + Geometry.BlockSize - 1) / Geometry.BlockSize;
            if ((DirectoryBlocks <= 0) || (DirectoryBlocks >= BlockCount))
                throw new InvalidOperationException("Invalid CP/M directory size");

            Root = new Node
            {
                Name = "",
                Path = "",
                Type = FilesystemItemType.Directory,
                Mode = 0x1ED
            };
            Nodes[""] = Root;
            BuildTree();
        }

        public static bool TryDetect(byte[] Data, string SourceName, out CpmGeometry Geometry) =>
            TryDetect(Data, SourceName, out Geometry, true);

        public static bool TryDetect(byte[] Data, string SourceName, out CpmGeometry Geometry, bool RequireFiles)
        {
            Geometry = null;
            if ((Data == null) || (Data.Length < 512))
                return false;

            if (!String.IsNullOrEmpty(SourceName) && !HasCpmExtension(SourceName))
                return false;

            CpmGeometry Best = null;
            int BestFiles = -1;
            foreach (var Candidate in Candidates())
            {
                if (Data.Length != Candidate.TotalBytes)
                    continue;

                try
                {
                    var Reader = new CpmReader(Data, Candidate);
                    int Files = Reader.Nodes.Values.Count(N => N.Type == FilesystemItemType.File);
                    if (Files > BestFiles)
                    {
                        BestFiles = Files;
                        Best = Candidate;
                    }
                }
                catch
                {
                }
            }

            if (Best == null)
                return false;
            if (RequireFiles && (BestFiles <= 0))
                return false;

            Geometry = Best;
            return true;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if ((Node == null) || (Node.Type != FilesystemItemType.File))
                return null;

            var Result = new byte[Node.Size];
            int HighestExtent = Node.Extents.Count == 0 ? -1 : Node.Extents.Keys.Max();
            for (int ExtentNo = 0; ExtentNo <= HighestExtent; ExtentNo++)
            {
                if (!Node.Extents.TryGetValue(ExtentNo, out var Extent))
                    continue;

                int Records = Extent.Records;
                if ((Records == 0) && Extent.Entry.Blocks.Any(B => B != 0))
                    Records = 128;

                for (int Record = 0; Record < Records; Record++)
                {
                    long Position = (long)ExtentNo * 16384 + Record * 128;
                    if (Position >= Result.Length)
                        break;

                    long Local = (long)Extent.LocalExtent * 16384 + Record * 128;
                    int BlockIndex = (int)(Local / Geometry.BlockSize);
                    int InBlock = (int)(Local % Geometry.BlockSize);
                    if ((BlockIndex < 0) || (BlockIndex >= Extent.Entry.Blocks.Length))
                        continue;

                    int Block = Extent.Entry.Blocks[BlockIndex];
                    if (Block == 0)
                        continue;

                    var BlockData = ReadBlock(Block);
                    int Count = (int)Math.Min(128, Math.Min(Result.Length - Position, Geometry.BlockSize - InBlock));
                    Array.Copy(BlockData, InBlock, Result, Position, Count);
                }
            }

            return Result;
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
                    (Child.Type == FilesystemItemType.Directory) ? 0u : Child.Size));
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
            var DirectoryBytes = ReadBlocks(0, DirectoryBlocks);
            var Groups = new Dictionary<string, List<DirEntry>>(StringComparer.Ordinal);

            for (int Index = 0; Index < Geometry.DirectoryEntries; Index++)
            {
                int Offset = Index * 32;
                if (Offset + 32 > DirectoryBytes.Length)
                    break;

                int Status = DirectoryBytes[Offset];
                if ((Status == 0xE5) || (Status > 31))
                    continue;

                int NameLength = 8;
                while ((NameLength > 0) && (DirectoryBytes[Offset + NameLength] == ' '))
                    NameLength--;
                if (NameLength == 0)
                    continue;

                var Name = DecodeField(DirectoryBytes, Offset + 1, 8);
                var Extension = DecodeField(DirectoryBytes, Offset + 9, 3);
                if (Name.Length == 0)
                    continue;

                var Entry = new DirEntry
                {
                    User = Status,
                    Name = Extension.Length == 0 ? Name : Name + "." + Extension,
                    Extent = ((DirectoryBytes[Offset + 14] & 0x3F) << 5) | (DirectoryBytes[Offset + 12] & 0x1F),
                    BytesInLastRecord = DirectoryBytes[Offset + 13],
                    Records = DirectoryBytes[Offset + 15],
                    ReadOnly = (DirectoryBytes[Offset + 9] & 0x80) != 0,
                    Blocks = ReadPointers(DirectoryBytes, Offset + 16)
                };

                var Key = Status.ToString() + ":" + Entry.Name;
                if (!Groups.TryGetValue(Key, out var List))
                {
                    List = new List<DirEntry>();
                    Groups[Key] = List;
                }
                List.Add(Entry);
            }

            var UserDirectories = new Dictionary<int, Node>();
            foreach (var Group in Groups.Values)
            {
                var Entry = Group[0];
                var Extents = new Dictionary<int, FileExtent>();
                int HighestExtent = -1;

                foreach (var Physical in Group)
                {
                    int FirstExtent = (Physical.Extent / ExtentsPerPhysical) * ExtentsPerPhysical;
                    for (int ExtentNo = FirstExtent; ExtentNo <= Physical.Extent; ExtentNo++)
                    {
                        bool Last = (ExtentNo == Physical.Extent);
                        Extents[ExtentNo] = new FileExtent
                        {
                            Entry = Physical,
                            LocalExtent = ExtentNo - FirstExtent,
                            Records = Last ? Physical.Records : 128,
                            BytesInLastRecord = Last ? Physical.BytesInLastRecord : 0
                        };
                        if (ExtentNo > HighestExtent)
                            HighestExtent = ExtentNo;
                    }
                }

                uint Size = 0;
                if (HighestExtent >= 0)
                {
                    var LastExtent = Extents[HighestExtent];
                    int LastBytes = LastExtent.Records == 0
                        ? (LastExtent.Entry.Blocks.Any(B => B != 0) ? 16384 : 0)
                        : (LastExtent.Records - 1) * 128 + (LastExtent.BytesInLastRecord == 0 ? 128 : LastExtent.BytesInLastRecord);
                    Size = (uint)(HighestExtent * 16384 + LastBytes);
                }

                uint Mode = Entry.ReadOnly ? 0x124u : 0x1A4u;
                if (Entry.Name.EndsWith(".COM", StringComparison.Ordinal))
                    Mode |= 0x49u;

                var Node = new Node
                {
                    Name = Entry.Name,
                    Type = FilesystemItemType.File,
                    User = Entry.User,
                    Size = Size,
                    Mode = Mode,
                    Extents = Extents
                };

                Node ParentNode;
                if (Entry.User == 0)
                {
                    ParentNode = Root;
                }
                else
                {
                    if (!UserDirectories.TryGetValue(Entry.User, out ParentNode))
                    {
                        ParentNode = new Node
                        {
                            Name = "u" + Entry.User.ToString("00"),
                            Path = "u" + Entry.User.ToString("00"),
                            Type = FilesystemItemType.Directory,
                            Mode = 0x1ED
                        };
                        UserDirectories[Entry.User] = ParentNode;
                        Nodes[ParentNode.Path] = ParentNode;
                        Root.Children.Add(ParentNode);
                    }
                }

                Node.Path = Combine(ParentNode.Path, Node.Name);
                Nodes[Node.Path] = Node;
                ParentNode.Children.Add(Node);
            }
        }

        private int[] ReadPointers(byte[] Entry, int Offset)
        {
            var Result = new int[PointersPerEntry];
            for (int I = 0; I < PointersPerEntry; I++)
            {
                if (BlockCount < 256)
                    Result[I] = Entry[Offset + I];
                else
                    Result[I] = Entry[Offset + I * 2] | (Entry[Offset + I * 2 + 1] << 8);
            }
            return Result;
        }

        private byte[] ReadBlocks(int First, int Count)
        {
            var Result = new byte[Count * Geometry.BlockSize];
            for (int I = 0; I < Count; I++)
                Array.Copy(ReadBlock(First + I), 0, Result, I * Geometry.BlockSize, Geometry.BlockSize);
            return Result;
        }

        private byte[] ReadBlock(int Block)
        {
            var Result = new byte[Geometry.BlockSize];
            int SectorsPerBlock = Geometry.BlockSize / Geometry.SectorSize;
            long FirstSector = (long)Block * SectorsPerBlock +
                (long)Geometry.ReservedTracks * Geometry.SectorsPerTrack;
            int Track = (int)(FirstSector / Geometry.SectorsPerTrack);
            int Sector = (int)(FirstSector % Geometry.SectorsPerTrack);

            for (int I = 0; I < SectorsPerBlock; I++)
            {
                long Offset = ((long)Track * Geometry.SectorsPerTrack + SkewTable[Sector]) * Geometry.SectorSize;
                if ((Offset < 0) || (Offset + Geometry.SectorSize > Data.Length))
                    throw new InvalidOperationException("CP/M block is outside the image");
                Array.Copy(Data, Offset, Result, I * Geometry.SectorSize, Geometry.SectorSize);
                Sector++;
                if (Sector >= Geometry.SectorsPerTrack)
                {
                    Sector = 0;
                    Track++;
                }
            }

            return Result;
        }

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
        }

        private static string DecodeField(byte[] Data, int Offset, int Length)
        {
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)(Data[Offset + I] & 0x7F);
            return new string(Chars).TrimEnd(' ');
        }

        private static int[] BuildSkewTable(CpmGeometry Geometry)
        {
            if (Geometry.SkewTable != null)
                return (int[])Geometry.SkewTable.Clone();

            int Count = Geometry.SectorsPerTrack;
            var Result = new int[Count];
            int Physical = 0;
            for (int I = 0; I < Count; I++)
            {
                while (Array.IndexOf(Result, Physical, 0, I) >= 0)
                    Physical = (Physical + 1) % Count;

                Result[I] = Physical;
                Physical = (Physical + Geometry.Skew) % Count;
            }
            return Result;
        }

        private static bool HasCpmExtension(string SourceName)
        {
            var Extension = System.IO.Path.GetExtension(SourceName);
            return String.Equals(Extension, ".dsk", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(Extension, ".cpm", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<CpmGeometry> Candidates()
        {
            yield return new CpmGeometry("ibm-3740", 128, 77, 26, 1024, 64, 6, 2);
            yield return new CpmGeometry("cpm-2k-40t", 128, 40, 20, 2048, 64, 1, 1);
            yield return new CpmGeometry("cpm-2k-210t", 128, 210, 20, 2048, 64, 1, 1);
            yield return new CpmGeometry("z80pack-hd", 128, 255, 128, 2048, 1024, 0, 0);
            yield return new CpmGeometry("4mb-hd", 128, 1024, 32, 2048, 256, 1, 0);
            yield return new CpmGeometry("sdcard", 512, 256, 64, 8192, 256, 0, 1);
            yield return new CpmGeometry("pcw", 512, 40, 9, 1024, 64, 1, 1);
            yield return new CpmGeometry("osborne1", 1024, 40, 5, 1024, 64, 1, 3);
        }

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
