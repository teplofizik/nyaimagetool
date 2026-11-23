using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only TRSDOS/LDOS directory reader for TRS-80 DMK images. The
    /// directory is scanned from track 20 (Model III/4) or track 17 (Model I),
    /// and file data is allocated in 3-sector granules on 18-sector disks.
    /// </summary>
    public sealed class TrsDosReader : IFilesystemReader
    {
        private const int HeaderSize = 16;
        private const int TrackHeaderSize = 128;
        private const int SectorSize = 256;
        private const int SectorsPerGranule = 3;
        private const int GranulesPerTrack = 6;
        private const int Model34DirectoryTrack = 20;
        private const int Model1DirectoryTrack = 17;
        private const int DirectoryFirstSector = 2;
        private const int DirectoryLastSector = 17;
        private const int EntrySize = 32;

        private sealed class FileEntry
        {
            public string Name;
            public string Path;
            public uint Size;
            public uint Mode;
            public byte[] Raw;
        }

        private readonly Dictionary<int, byte[]>[] Tracks;
        private readonly int TrackCount;
        private readonly Node Root;
        private readonly Dictionary<string, FileEntry> Files = new Dictionary<string, FileEntry>(StringComparer.Ordinal);

        private sealed class Node
        {
            public string Name;
            public string Path;
            public FilesystemItemType Type;
            public uint Size;
            public uint Mode;
            public FileEntry File;
            public readonly List<Node> Children = new List<Node>();
        }

        public long VolumeBytes { get; }

        public TrsDosReader(byte[] Data)
        {
            if (!TryParse(Data, out var Tracks, out var TrackCount, out var DirectoryTrack))
                throw new InvalidOperationException("Not a supported TRS-80 TRSDOS/LDOS DMK image");

            this.Tracks = Tracks;
            this.TrackCount = TrackCount;
            VolumeBytes = (long)TrackCount * 18 * SectorSize;

            Root = new Node
            {
                Name = "",
                Path = "",
                Type = FilesystemItemType.Directory,
                Mode = 0x1ED
            };

            var Entries = ReadDirectory(DirectoryTrack);
            foreach (var Entry in Entries)
            {
                var File = BuildFile(Entry);
                if (File == null)
                    continue;

                var Node = new Node
                {
                    Name = File.Name,
                    Path = File.Path,
                    Type = FilesystemItemType.File,
                    Size = File.Size,
                    Mode = File.Mode,
                    File = File
                };
                Files[Node.Path] = File;
                Root.Children.Add(Node);
            }
        }

        public static bool TryDetect(byte[] Data, string SourceName)
        {
            if (!HasDmkExtension(SourceName))
                return false;
            if (!TryParse(Data, out var Tracks, out var TrackCount, out var DirectoryTrack))
                return false;

            var Reader = new TrsDosReader(Tracks, TrackCount, DirectoryTrack);
            return Reader.Files.ContainsKey("BOOT.SYS") || Reader.Files.ContainsKey("DIR.SYS");
        }

        private TrsDosReader(Dictionary<int, byte[]>[] Tracks, int TrackCount, int DirectoryTrack)
        {
            this.Tracks = Tracks;
            this.TrackCount = TrackCount;
            VolumeBytes = (long)TrackCount * 18 * SectorSize;
            Root = new Node
            {
                Name = "",
                Path = "",
                Type = FilesystemItemType.Directory,
                Mode = 0x1ED
            };

            foreach (var Entry in ReadDirectory(DirectoryTrack))
            {
                var File = BuildFile(Entry);
                if (File == null)
                    continue;

                Files[File.Path] = File;
                Root.Children.Add(new Node
                {
                    Name = File.Name,
                    Path = File.Path,
                    Type = FilesystemItemType.File,
                    Size = File.Size,
                    Mode = File.Mode,
                    File = File
                });
            }
        }

        public byte[] Read(string Path)
        {
            var Key = Normalize(Path);
            return Files.TryGetValue(Key, out var File) ? ReadFile(File) : null;
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

        private List<byte[]> ReadDirectory(int Track)
        {
            var Result = new List<byte[]>();
            for (int Sector = DirectoryFirstSector; Sector <= DirectoryLastSector; Sector++)
            {
                var Data = ReadSector(Track, Sector);
                if (Data == null)
                    continue;

                for (int Offset = 0; Offset + EntrySize <= Data.Length; Offset += EntrySize)
                {
                    var Entry = new byte[EntrySize];
                    Array.Copy(Data, Offset, Entry, 0, EntrySize);
                    if (IsValidEntry(Entry))
                        Result.Add(Entry);
                }
            }
            return Result;
        }

        private FileEntry BuildFile(byte[] Entry)
        {
            var Name = DecodeField(Entry, 5, 8);
            var Extension = DecodeField(Entry, 13, 3);
            if (Name.Length == 0)
                return null;

            var FullName = Extension.Length == 0 ? Name : Name + "." + Extension;
            var Extents = ReadExtents(Entry);
            int AllocatedSectors = 0;
            foreach (var Extent in Extents)
                AllocatedSectors += (Extent.Value & 0xFF) * SectorsPerGranule;

            uint Size = ReadSize(Entry, AllocatedSectors);
            uint Mode = ((Entry[0] & 0x40) != 0) ? 0x124u : 0x1A4u;

            return new FileEntry
            {
                Name = Sanitize(FullName),
                Path = Sanitize(FullName),
                Size = Size,
                Mode = Mode,
                Raw = Entry
            };
        }

        private byte[] ReadFile(FileEntry File)
        {
            var Result = new byte[File.Size];
            int Written = 0;
            foreach (var Extent in ReadExtents(File.Raw))
            {
                int Track = Extent.Key;
                int FirstGranule = (Extent.Value >> 8) & 0xFF;
                int Count = Extent.Value & 0xFF;

                for (int Granule = 0; Granule < Count; Granule++)
                {
                    int StartSector = (FirstGranule + Granule) * SectorsPerGranule;
                    for (int SectorOffset = 0; SectorOffset < SectorsPerGranule; SectorOffset++)
                    {
                        var Data = ReadSector(Track, StartSector + SectorOffset);
                        if (Data == null)
                            continue;

                        int Copy = Math.Min(Data.Length, Result.Length - Written);
                        if (Copy <= 0)
                            return Result;

                        Array.Copy(Data, 0, Result, Written, Copy);
                        Written += Copy;
                    }
                }
            }
            return Result;
        }

        private List<KeyValuePair<int, int>> ReadExtents(byte[] Entry)
        {
            var Result = new List<KeyValuePair<int, int>>();
            for (int Index = 0; Index < 5; Index++)
            {
                int Offset = 22 + Index * 2;
                int Track = Entry[Offset];
                int Info = Entry[Offset + 1];
                if (Track >= 0xFF)
                    break;
                if ((Track >= TrackCount) || (Track == 0xFF))
                    break;

                int FirstGranule = (Info >> 5) & 0x07;
                int Count = (Info & 0x1F) + 1;
                if (FirstGranule + Count > GranulesPerTrack)
                    Count = Math.Max(1, GranulesPerTrack - FirstGranule);

                Result.Add(new KeyValuePair<int, int>(Track, (FirstGranule << 8) | Count));
            }
            return Result;
        }

        private static uint ReadSize(byte[] Entry, int AllocatedSectors)
        {
            byte EofLow = Entry[3];
            int EofMid = Entry[20];
            int EofHigh = Entry[21];

            if (EofLow == 0)
            {
                if (AllocatedSectors == 0)
                    return 0;

                int LastOffset = (EofHigh << 8) | EofMid;
                return (uint)((AllocatedSectors - 1) * SectorSize + LastOffset + 1);
            }

            int RawEof = (EofHigh << 16) | (EofMid << 8) | EofLow;
            return (uint)(RawEof >= 255 ? RawEof - 255 : RawEof);
        }

        private static bool IsValidEntry(byte[] Entry)
        {
            int Attr = Entry[0];
            if ((Attr == 0) || (Attr == 0xFF) || ((Attr & 0x80) != 0))
                return false;

            for (int I = 5; I < 13; I++)
            {
                if ((Entry[I] < 32) || (Entry[I] > 126))
                    return false;
            }
            for (int I = 13; I < 16; I++)
            {
                if ((Entry[I] < 32) || (Entry[I] > 126))
                    return false;
            }

            char First = (char)Entry[5];
            return (First != ' ') && (First != '\0');
        }

        private byte[] ReadSector(int Track, int Sector)
        {
            if ((Track < 0) || (Track >= TrackCount))
                return null;
            return Tracks[Track].TryGetValue(Sector, out var Data) ? Data : null;
        }

        private static bool TryParse(byte[] Data, out Dictionary<int, byte[]>[] Tracks,
            out int TrackCount, out int DirectoryTrack)
        {
            Tracks = null;
            TrackCount = 0;
            DirectoryTrack = 0;

            if ((Data == null) || (Data.Length < HeaderSize + TrackHeaderSize))
                return false;

            TrackCount = Data[1];
            int TrackLength = ReadLe16(Data, 2);
            if ((TrackCount <= 0) || (TrackCount > 100) || (TrackLength < TrackHeaderSize + SectorSize))
                return false;
            if (Data.Length < HeaderSize + (long)TrackCount * TrackLength)
                return false;

            Tracks = new Dictionary<int, byte[]>[TrackCount];
            for (int Track = 0; Track < TrackCount; Track++)
            {
                Tracks[Track] = ReadTrack(Data, HeaderSize + Track * TrackLength, TrackLength);
                if (Tracks[Track].Count == 0)
                    return false;
            }

            if (HasDirectory(Tracks, Model34DirectoryTrack))
                DirectoryTrack = Model34DirectoryTrack;
            else if (HasDirectory(Tracks, Model1DirectoryTrack))
                DirectoryTrack = Model1DirectoryTrack;
            else
                return false;

            return true;
        }

        private static bool HasDirectory(Dictionary<int, byte[]>[] Tracks, int Track)
        {
            if ((Track < 0) || (Track >= Tracks.Length))
                return false;

            int Entries = 0;
            for (int Sector = DirectoryFirstSector; Sector <= DirectoryLastSector; Sector++)
            {
                if (!Tracks[Track].TryGetValue(Sector, out var Data))
                    continue;

                for (int Offset = 0; Offset + EntrySize <= Data.Length; Offset += EntrySize)
                {
                    var Entry = new byte[EntrySize];
                    Array.Copy(Data, Offset, Entry, 0, EntrySize);
                    if (IsValidEntry(Entry))
                        Entries++;
                }
            }
            return Entries >= 2;
        }

        private static Dictionary<int, byte[]> ReadTrack(byte[] Data, int TrackBase, int TrackLength)
        {
            var Result = new Dictionary<int, byte[]>();
            int TrackEnd = TrackBase + TrackLength;

            for (int Pointer = 0; Pointer < TrackHeaderSize / 2; Pointer++)
            {
                int IdamOffset = ReadLe16(Data, TrackBase + Pointer * 2) & 0x3FFF;
                int Idam = TrackBase + IdamOffset;
                if ((IdamOffset == 0) || (Idam + 7 > TrackEnd) || (Data[Idam] != 0xFE))
                    continue;

                int Sector = Data[Idam + 3];
                int SizeCode = Data[Idam + 4];
                if ((Sector < 0) || (Sector >= 64) || (SizeCode > 3))
                    continue;

                int Size = 128 << SizeCode;
                int DataMark = FindDataMark(Data, Idam + 7, Math.Min(TrackEnd, Idam + 7 + 96));
                if ((DataMark < 0) || (DataMark + 1 + Size > TrackEnd))
                    continue;

                if (Size != SectorSize)
                    continue;

                var SectorData = new byte[Size];
                Array.Copy(Data, DataMark + 1, SectorData, 0, Size);
                if (!Result.ContainsKey(Sector))
                    Result[Sector] = SectorData;
            }

            return Result;
        }

        private static int FindDataMark(byte[] Data, int Start, int End)
        {
            for (int Offset = Start; Offset < End; Offset++)
            {
                if ((Data[Offset] == 0xFB) || (Data[Offset] == 0xF8))
                    return Offset;
            }
            return -1;
        }

        private static string DecodeField(byte[] Data, int Offset, int Length)
        {
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)(Data[Offset + I] & 0x7F);
            return new string(Chars).TrimEnd(' ');
        }

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            if (Key.Length == 0)
                return Root;
            foreach (var Child in Root.Children)
            {
                if (String.Equals(Child.Path, Key, StringComparison.Ordinal))
                    return Child;
            }
            return null;
        }

        private static bool HasDmkExtension(string SourceName)
        {
            if (String.IsNullOrEmpty(SourceName))
                return false;
            return String.Equals(Path.GetExtension(SourceName), ".dmk", StringComparison.OrdinalIgnoreCase);
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

        private static int ReadLe16(byte[] Data, int Offset) =>
            Data[Offset] | (Data[Offset + 1] << 8);
    }
}
