using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only Tandy Color Computer Disk BASIC (DECB) reader for raw .dsk
    /// images. Files are chained through the granule map on directory track 17.
    /// </summary>
    public sealed class CocoDosReader : IFilesystemReader
    {
        private const int SectorSize = 256;
        private const int SectorsPerTrack = 18;
        private const int DirectoryTrack = 17;
        private const int GranuleMapSector = 2;
        private const int DirectoryFirstSector = 3;
        private const int DirectoryLastSector = 11;
        private const int SectorsPerGranule = 9;
        private const int BytesPerGranule = SectorsPerGranule * SectorSize;
        private const int FreeGranule = 0xFF;

        private sealed class FileEntry
        {
            public string Name;
            public uint Size;
            public uint Mode;
            public byte[] Raw;
        }

        private sealed class Node
        {
            public string Name;
            public FilesystemItemType Type;
            public uint Size;
            public uint Mode;
            public FileEntry File;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly int Tracks;
        private readonly int GranuleMapOffset;
        private readonly Node Root;
        private readonly Dictionary<string, FileEntry> Files = new Dictionary<string, FileEntry>(StringComparer.Ordinal);

        public long VolumeBytes => Data.Length;

        public CocoDosReader(byte[] Data)
        {
            if (!TryParse(Data, out var Tracks, out var DirectoryOffset, out var GranuleMapOffset))
                throw new InvalidOperationException("Not a CoCo Disk BASIC image");

            this.Data = Data;
            this.Tracks = Tracks;
            this.GranuleMapOffset = GranuleMapOffset;

            Root = new Node
            {
                Name = "",
                Type = FilesystemItemType.Directory,
                Mode = 0x1ED
            };

            for (int Sector = DirectoryFirstSector; Sector <= DirectoryLastSector; Sector++)
            {
                int SectorOffset = DirectoryOffset + (Sector - 1) * SectorSize;
                for (int EntryOffset = 0; EntryOffset + 32 <= SectorSize; EntryOffset += 32)
                {
                    var Entry = new byte[32];
                    Array.Copy(Data, SectorOffset + EntryOffset, Entry, 0, 32);
                    var File = BuildFile(Entry, GranuleMapOffset);
                    if (File == null)
                        continue;

                    Files[File.Name] = File;
                    Root.Children.Add(new Node
                    {
                        Name = File.Name,
                        Type = FilesystemItemType.File,
                        Size = File.Size,
                        Mode = File.Mode,
                        File = File
                    });
                }
            }
        }

        public static bool TryDetect(byte[] Data, string SourceName)
        {
            if (!HasDskExtension(SourceName))
                return false;
            if (!TryParse(Data, out var Tracks, out var DirectoryOffset, out var GranuleMapOffset))
                return false;

            for (int Sector = DirectoryFirstSector; Sector <= DirectoryLastSector; Sector++)
            {
                int SectorOffset = DirectoryOffset + (Sector - 1) * SectorSize;
                for (int EntryOffset = 0; EntryOffset + 32 <= SectorSize; EntryOffset += 32)
                {
                    var Entry = new byte[32];
                    Array.Copy(Data, SectorOffset + EntryOffset, Entry, 0, 32);
                    if (IsValidEntry(Entry))
                        return true;
                }
            }
            return false;
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
            if (Normalize(Path).Length != 0)
                return Array.Empty<FilesystemEntry>();

            var Result = new List<FilesystemEntry>();
            foreach (var Child in Root.Children)
                Result.Add(new FilesystemEntry(Child.Type, Child.Name, 0, 0, Child.Mode, Child.Size));
            return Result.ToArray();
        }

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode)
        {
            User = 0;
            Group = 0;
            Mode = 0x1ED;
            return true;
        }

        private FileEntry BuildFile(byte[] Entry, int GranuleMapOffset)
        {
            if (!IsValidEntry(Entry))
                return null;

            var Name = DecodeField(Entry, 0, 8);
            var Extension = DecodeField(Entry, 8, 3);
            var FullName = Extension.Length == 0 ? Name : Name + "." + Extension;
            int First = Entry[13];
            int MaxGranules = (Tracks - 1) * 2;
            if ((First < 0) || (First >= MaxGranules))
                return null;

            var Granules = new List<int>();
            int Current = First;
            int LastSectors = 0;
            int Guard = 0;
            while ((Current >= 0) && (Current < MaxGranules) && (Guard++ < MaxGranules + 8))
            {
                Granules.Add(Current);
                int Next = Data[GranuleMapOffset + Current];
                if ((Next >= 0xC0) && (Next <= 0xC9))
                {
                    LastSectors = Next & 0x0F;
                    break;
                }
                if (Next == FreeGranule)
                    break;
                if (Next >= MaxGranules)
                    break;
                Current = Next;
            }

            uint Size = 0;
            if (LastSectors > 0)
            {
                uint LastBytes = (uint)((Entry[14] << 8) | Entry[15]);
                if (LastBytes > SectorSize)
                    LastBytes = SectorSize;
                Size = (uint)(Granules.Count - 1) * BytesPerGranule +
                    (uint)LastSectors * SectorSize - (SectorSize - LastBytes);
            }
            else
            {
                Size = (uint)Granules.Count * BytesPerGranule;
            }

            return new FileEntry
            {
                Name = Sanitize(FullName),
                Size = Size,
                Mode = 0x1A4,
                Raw = Entry
            };
        }

        private byte[] ReadFile(FileEntry File)
        {
            var Result = new byte[File.Size];
            int Written = 0;
            var Entry = File.Raw;
            int First = Entry[13];
            int Current = First;
            int MaxGranules = (Tracks - 1) * 2;
            int Guard = 0;
            while ((Current >= 0) && (Current < MaxGranules) && (Guard++ < MaxGranules + 8))
            {
                int Offset = GranuleOffset(Current);
                int Count = BytesPerGranule;
                int Next = GetGranuleNext(Current);
                if ((Next >= 0xC0) && (Next <= 0xC9))
                    Count = (Next & 0x0F) * SectorSize;

                int Copy = Math.Min(Count, Result.Length - Written);
                if (Copy > 0)
                {
                    Array.Copy(Data, Offset, Result, Written, Copy);
                    Written += Copy;
                }
                if (Written >= Result.Length)
                    break;
                if ((Next >= 0xC0) || (Next == FreeGranule) || (Next >= MaxGranules))
                    break;
                Current = Next;
            }
            return Result;
        }

        private int GetGranuleNext(int Granule) => Data[GranuleMapOffset + Granule];

        private int GranuleOffset(int Granule)
        {
            int Track = Granule / 2;
            if (Track >= DirectoryTrack)
                Track++;
            return Track * SectorsPerTrack * SectorSize + (Granule & 1) * BytesPerGranule;
        }

        private static bool IsValidEntry(byte[] Entry)
        {
            if (Entry[11] > 3)
                return false;
            if (Entry[11] == 0xFF)
                return false;

            for (int I = 0; I < 8; I++)
            {
                if ((Entry[I] < 32) || (Entry[I] > 126))
                    return false;
            }
            for (int I = 8; I < 11; I++)
            {
                if ((Entry[I] < 32) || (Entry[I] > 126))
                    return false;
            }
            return Entry[0] != ' ';
        }

        private static bool TryParse(byte[] Data, out int Tracks, out int DirectoryOffset,
            out int GranuleMapOffset)
        {
            Tracks = 0;
            DirectoryOffset = 0;
            GranuleMapOffset = 0;

            if ((Data == null) || (Data.Length < 35L * SectorsPerTrack * SectorSize))
                return false;
            if (Data.Length % (SectorsPerTrack * SectorSize) != 0)
                return false;

            Tracks = Data.Length / (SectorsPerTrack * SectorSize);
            if ((Tracks != 35) && (Tracks != 40))
                return false;

            DirectoryOffset = DirectoryTrack * SectorsPerTrack * SectorSize;
            GranuleMapOffset = DirectoryOffset + (GranuleMapSector - 1) * SectorSize;
            if (GranuleMapOffset + Tracks * 2 > Data.Length)
                return false;

            return true;
        }

        private static bool HasDskExtension(string SourceName)
        {
            if (String.IsNullOrEmpty(SourceName))
                return false;
            return String.Equals(Path.GetExtension(SourceName), ".dsk", StringComparison.OrdinalIgnoreCase);
        }

        private static string DecodeField(byte[] Data, int Offset, int Length)
        {
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)(Data[Offset + I] & 0x7F);
            return new string(Chars).TrimEnd(' ');
        }

        private static string Sanitize(string Name) =>
            Name.Replace('/', '_').Replace('\\', '_');

        private static string Normalize(string Path)
        {
            if (String.IsNullOrEmpty(Path) || (Path == ".") || (Path == "/"))
                return "";
            return Path.Replace('\\', '/').Trim('/');
        }
    }
}
