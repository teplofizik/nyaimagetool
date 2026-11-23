using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only TRS-80 DMK container backed by a CP/M filesystem. DMK itself
    /// only supplies raw tracks and sector IDs; the CP/M geometry below is the
    /// standard 40-track, 18-sector, 256-byte Model 4 format.
    /// </summary>
    public sealed class DmkReader : IFilesystemReader
    {
        private const int HeaderSize = 16;
        private const int TrackHeaderSize = 128;
        private const int TrackCount = 40;
        private const int SectorsPerTrack = 18;
        private const int SectorSize = 256;
        private const int ReservedTracks = 2;
        private const int BlockSize = 2048;
        private const int DirectoryEntries = 128;

        private static readonly int[] Trs80Skew =
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16,
            1, 3, 5, 7, 9, 11, 13, 15, 17
        };

        private readonly CpmReader Inner;

        public CpmGeometry Geometry => Inner.Geometry;
        public long VolumeBytes => Inner.VolumeBytes;

        public DmkReader(byte[] Data)
        {
            if (!TryExtract(Data, out var CpmImage, out var Geometry))
                throw new InvalidOperationException("Not a supported TRS-80 DMK/CP/M image");

            Inner = new CpmReader(CpmImage, Geometry);
        }

        public static bool TryDetect(byte[] Data, string SourceName, out CpmGeometry Geometry)
        {
            Geometry = null;
            if (!HasDmkExtension(SourceName))
                return false;
            if (!TryExtract(Data, out var CpmImage, out var FoundGeometry))
                return false;

            try
            {
                var Reader = new CpmReader(CpmImage, FoundGeometry);
                if (Reader.ReadDir("/").Length == 0)
                    return false;

                Geometry = FoundGeometry;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryExtract(byte[] Data, out byte[] CpmImage, out CpmGeometry Geometry)
        {
            CpmImage = null;
            Geometry = null;

            if ((Data == null) || (Data.Length < HeaderSize + TrackHeaderSize))
                return false;

            int Tracks = Data[1];
            int TrackLength = ReadLe16(Data, 2);
            if ((Tracks != TrackCount) || (TrackLength < TrackHeaderSize + 128))
                return false;
            if (Data.Length < HeaderSize + (long)Tracks * TrackLength)
                return false;

            var Sectors = new Dictionary<int, byte[]>[Tracks];
            for (int Track = 0; Track < Tracks; Track++)
            {
                int TrackBase = HeaderSize + Track * TrackLength;
                Sectors[Track] = ReadTrack(Data, TrackBase, TrackLength);
            }

            if (!HasGeometry(Sectors))
                return false;

            var Result = new byte[TrackCount * SectorsPerTrack * SectorSize];
            Array.Fill(Result, (byte)0xE5);
            for (int Track = 0; Track < TrackCount; Track++)
            {
                foreach (var Pair in Sectors[Track])
                {
                    int Index = Pair.Key - 1;
                    if ((Index < 0) || (Index >= SectorsPerTrack) || (Pair.Value.Length != SectorSize))
                        continue;

                    Array.Copy(Pair.Value, 0, Result,
                        (Track * SectorsPerTrack + Index) * SectorSize, SectorSize);
                }
            }

            Geometry = new CpmGeometry("trs80-dmk", SectorSize, TrackCount, SectorsPerTrack,
                BlockSize, DirectoryEntries, 0, ReservedTracks, Trs80Skew);
            CpmImage = Result;
            return true;
        }

        public byte[] Read(string Path) => Inner.Read(Path);

        public string ReadLink(string Path) => Inner.ReadLink(Path);

        public DeviceInfo ReadDevice(string Path) => Inner.ReadDevice(Path);

        public FilesystemEntry[] ReadDir(string Path) => Inner.ReadDir(Path);

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode) =>
            Inner.TryGetRootFsItem(out User, out Group, out Mode);

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
                if ((Sector <= 0) || (Sector > 64) || (SizeCode > 3))
                    continue;

                int Size = 128 << SizeCode;
                int DataMark = FindDataMark(Data, Idam + 7, Math.Min(TrackEnd, Idam + 7 + 96));
                if ((DataMark < 0) || (DataMark + 1 + Size > TrackEnd))
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

        private static bool HasGeometry(Dictionary<int, byte[]>[] Sectors)
        {
            for (int Track = 0; Track < Sectors.Length; Track++)
            {
                for (int Sector = 1; Sector <= SectorsPerTrack; Sector++)
                {
                    if (!Sectors[Track].TryGetValue(Sector, out var Data) ||
                        (Data.Length != SectorSize))
                        return false;
                }
            }
            return true;
        }

        private static bool HasDmkExtension(string SourceName)
        {
            if (String.IsNullOrEmpty(SourceName))
                return false;
            return String.Equals(Path.GetExtension(SourceName), ".dmk", StringComparison.OrdinalIgnoreCase);
        }

        private static int ReadLe16(byte[] Data, int Offset) =>
            Data[Offset] | (Data[Offset + 1] << 8);
    }
}
