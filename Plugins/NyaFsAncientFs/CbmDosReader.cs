using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only Commodore DOS reader for standard 35-track .d64 images.
    /// CBM DOS is flat: all files live in the root namespace.
    /// </summary>
    public sealed class CbmDosReader : IFilesystemReader
    {
        private const int SectorSize = 256;
        private const int DirectoryTrack = 18;
        private const int DirectorySector = 1;
        private static readonly int[] SectorCounts = BuildSectorCounts();

        private sealed class Node
        {
            public string Name;
            public byte FileType;
            public byte FirstTrack;
            public byte FirstSector;
            public uint Blocks;
            public uint Size;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly Node Root;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);

        public long VolumeBytes => Data.Length;

        public CbmDosReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsCbmDos(this.Data, null))
                throw new InvalidOperationException("Not a Commodore .d64 filesystem");

            Root = new Node { Name = "", FileType = 0 };
            Nodes[""] = Root;
            BuildTree();
        }

        public static bool IsCbmDos(byte[] Data, string SourceName)
        {
            if ((Data == null) || (Data.Length < 174848))
                return false;
            if (!String.IsNullOrEmpty(SourceName) &&
                !SourceName.EndsWith(".d64", StringComparison.OrdinalIgnoreCase))
                return false;

            int Bam;
            if (!TryOffset(DirectoryTrack, 0, out Bam) || (Bam + 4 > Data.Length))
                return false;
            if ((Data[Bam] != DirectoryTrack) || (Data[Bam + 1] != DirectorySector))
                return false;

            int Dir;
            if (!TryOffset(DirectoryTrack, DirectorySector, out Dir) || (Dir + 2 > Data.Length))
                return false;

            return Data[Dir + 0] != 0 || Data[Dir + 1] != 0;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if (Node == null)
                return null;
            return ReadChain(Node.FirstTrack, Node.FirstSector);
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
                    0x1A4,
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
            int Track = DirectoryTrack;
            int Sector = DirectorySector;
            int Guard = 0;

            while ((Track != 0) && (Guard++ < 256))
            {
                int Offset;
                if (!TryOffset(Track, Sector, out Offset) || (Offset + SectorSize > Data.Length))
                    break;

                int NextTrack = Data[Offset];
                int NextSector = Data[Offset + 1];

                for (int I = 0; I < 8; I++)
                {
                    int Entry = Offset + 2 + I * 32;
                    if (Entry + 32 > Data.Length)
                        break;

                    byte Type = Data[Entry];
                    byte FileType = (byte)(Type & 0x0F);
                    if (FileType == 0)
                        continue;

                    string Name = DecodeName(Data, Entry + 3, 16);
                    if (Name.Length == 0)
                        continue;

                    var Node = new Node
                    {
                        Name = Sanitize(Name),
                        FileType = FileType,
                        FirstTrack = Data[Entry + 1],
                        FirstSector = Data[Entry + 2],
                        Blocks = (uint)(Data[Entry + 28] | (Data[Entry + 29] << 8))
                    };
                    Node.Size = (uint)ReadChain(Node.FirstTrack, Node.FirstSector).Length;

                    Root.Children.Add(Node);
                    Nodes[Node.Name] = Node;
                }

                Track = NextTrack;
                Sector = NextSector;
            }
        }

        private byte[] ReadChain(byte Track, byte Sector)
        {
            var Result = new List<byte>();
            int Guard = 0;

            while ((Track != 0) && (Guard++ < 1024))
            {
                int Offset;
                if (!TryOffset(Track, Sector, out Offset) || (Offset + SectorSize > Data.Length))
                    break;

                int NextTrack = Data[Offset];
                int NextSector = Data[Offset + 1];
                for (int I = 2; I < SectorSize; I++)
                    Result.Add(Data[Offset + I]);

                if ((NextTrack == 0) && (NextSector == 0))
                    break;

                Track = (byte)NextTrack;
                Sector = (byte)NextSector;
            }

            return Result.ToArray();
        }

        private static bool TryOffset(int Track, int Sector, out int Offset)
        {
            Offset = 0;
            if ((Track < 1) || (Track > SectorCounts.Length) ||
                (Sector < 0) || (Sector >= SectorCounts[Track - 1]))
                return false;

            int Sectors = 0;
            for (int I = 0; I < Track - 1; I++)
                Sectors += SectorCounts[I];

            Offset = (Sectors + Sector) * SectorSize;
            return true;
        }

        private static int[] BuildSectorCounts()
        {
            var Result = new int[40];
            for (int Track = 1; Track <= Result.Length; Track++)
                Result[Track - 1] = (Track <= 17) ? 21 : ((Track <= 24) ? 19 : ((Track <= 30) ? 18 : 17));
            return Result;
        }

        private static string DecodeName(byte[] Data, int Offset, int Length)
        {
            int End = Offset;
            while ((End < Offset + Length) && (Data[End] != 0xA0) && (Data[End] != 0))
                End++;

            var Chars = new char[End - Offset];
            for (int I = 0; I < Chars.Length; I++)
            {
                byte B = Data[Offset + I];
                if ((B >= 0x41) && (B <= 0x5A))
                    Chars[I] = (char)('A' + (B - 0x41));
                else if ((B >= 0xC1) && (B <= 0xDA))
                    Chars[I] = (char)('a' + (B - 0xC1));
                else if ((B >= 0x20) && (B <= 0x7E))
                    Chars[I] = (char)B;
                else
                    Chars[I] = '?';
            }
            return new string(Chars).TrimEnd();
        }

        private static string Sanitize(string Name) =>
            Name.Replace('/', '_').Replace('\\', '_');

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
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
