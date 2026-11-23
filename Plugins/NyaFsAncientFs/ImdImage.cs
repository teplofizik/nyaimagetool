using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsAncientFs
{
    /// <summary>
    /// ImageDisk (.imd) parser. Converts the track/sector records into a flat
    /// sector image when all tracks use the same geometry.
    /// </summary>
    public sealed class ImdImage
    {
        private const byte EndOfHeader = 0x1A;
        private const int MaxSectors = 256;

        public readonly int Tracks;
        public readonly int Sides;
        public readonly int SectorsPerTrack;
        public readonly int SectorSize;
        public readonly byte[] Raw;

        private ImdImage(int Tracks, int Sides, int SectorsPerTrack, int SectorSize, byte[] Raw)
        {
            this.Tracks = Tracks;
            this.Sides = Sides;
            this.SectorsPerTrack = SectorsPerTrack;
            this.SectorSize = SectorSize;
            this.Raw = Raw;
        }

        public static bool TryRead(byte[] Data, out ImdImage Image)
        {
            Image = null;
            if ((Data == null) || (Data.Length < 6))
                return false;

            int Pos = 0;
            while ((Pos < Data.Length) && (Data[Pos] != EndOfHeader))
                Pos++;
            if (Pos >= Data.Length)
                return false;
            Pos++;

            var TrackList = new List<Track>();
            int MaxCylinder = -1;
            int MaxHead = -1;
            int SectorSize = -1;
            int SectorsPerTrack = -1;

            while (Pos < Data.Length)
            {
                if (Pos + 5 > Data.Length)
                    break;

                int Mode = Data[Pos++];
                int Cylinder = Data[Pos++];
                int Head = Data[Pos++];
                int Sectors = Data[Pos++];
                int SizeCode = Data[Pos++];
                if ((Sectors <= 0) || (Sectors > MaxSectors))
                    return false;

                int SectorSizeValue;
                int[] Sizes;
                if (SizeCode == 0xFF)
                {
                    Sizes = new int[Sectors];
                    SectorSizeValue = 0;
                }
                else
                {
                    SectorSizeValue = 128 << SizeCode;
                    Sizes = new int[Sectors];
                    for (int I = 0; I < Sectors; I++)
                        Sizes[I] = SectorSizeValue;
                }

                if ((Head & 0x80) != 0)
                {
                    if (Pos + Sectors > Data.Length) return false;
                    Pos += Sectors;
                }
                if ((Head & 0x40) != 0)
                {
                    if (Pos + Sectors > Data.Length) return false;
                    Pos += Sectors;
                }

                if (Pos + Sectors > Data.Length)
                    return false;
                var Ids = new int[Sectors];
                for (int I = 0; I < Sectors; I++)
                    Ids[I] = Data[Pos++];

                if (SizeCode == 0xFF)
                {
                    if (Pos + Sectors > Data.Length)
                        return false;
                    for (int I = 0; I < Sectors; I++)
                    {
                        int Code = Data[Pos++];
                        if (Code > 6)
                            return false;
                        Sizes[I] = 128 << Code;
                    }
                }

                if (SectorSizeValue == 0)
                {
                    SectorSizeValue = Sizes[0];
                    for (int I = 1; I < Sectors; I++)
                        if (Sizes[I] != SectorSizeValue)
                            return false;
                }
                if (SectorSizeValue <= 0)
                    return false;

                if (SectorsPerTrack < 0)
                {
                    SectorsPerTrack = Sectors;
                    SectorSize = SectorSizeValue;
                }
                else if ((SectorsPerTrack != Sectors) || (SectorSize != SectorSizeValue))
                {
                    return false;
                }

                var SectorData = new Dictionary<int, byte[]>();
                for (int I = 0; I < Sectors; I++)
                {
                    if (Pos >= Data.Length)
                        return false;

                    int Status = Data[Pos++];
                    byte[] Payload;
                    switch (Status)
                    {
                        case 0:
                            Payload = null;
                            break;
                        case 1:
                        case 3:
                        case 4:
                        case 6:
                            if (Pos + Sizes[I] > Data.Length)
                                return false;
                            Payload = new byte[Sizes[I]];
                            Array.Copy(Data, Pos, Payload, 0, Sizes[I]);
                            Pos += Sizes[I];
                            break;
                        case 2:
                        case 5:
                            if (Pos + 1 > Data.Length)
                                return false;
                            Payload = new byte[Sizes[I]];
                            Array.Fill(Payload, Data[Pos++]);
                            break;
                        default:
                            return false;
                    }

                    if ((Ids[I] > 0) && (Payload != null))
                        SectorData[Ids[I]] = Payload;
                }

                TrackList.Add(new Track(Cylinder, Head & 0x3F, Sectors, SectorData));
                MaxCylinder = Math.Max(MaxCylinder, Cylinder);
                MaxHead = Math.Max(MaxHead, Head & 0x3F);
            }

            if ((TrackList.Count == 0) || (MaxCylinder < 0) || (MaxHead < 0) ||
                (SectorsPerTrack <= 0) || (SectorSize <= 0))
                return false;

            int TrackCount = MaxCylinder + 1;
            int SideCount = MaxHead + 1;
            long Total = (long)TrackCount * SideCount * SectorsPerTrack * SectorSize;
            if ((Total <= 0) || (Total > 256L * 1024 * 1024))
                return false;

            var Raw = new byte[Total];
            Array.Fill(Raw, (byte)0xE5);
            foreach (var Record in TrackList)
            {
                foreach (var Pair in Record.Data)
                {
                    int Index = Pair.Key - 1;
                    if ((Index < 0) || (Index >= SectorsPerTrack) ||
                        (Pair.Value.Length != SectorSize))
                        continue;

                    long Offset = ((long)Record.Cylinder * SideCount + Record.Head) *
                        SectorsPerTrack * SectorSize + (long)Index * SectorSize;
                    Array.Copy(Pair.Value, 0, Raw, Offset, SectorSize);
                }
            }

            Image = new ImdImage(TrackCount, SideCount, SectorsPerTrack, SectorSize, Raw);
            return true;
        }

        private sealed class Track
        {
            public readonly int Cylinder;
            public readonly int Head;
            public readonly int Sectors;
            public readonly Dictionary<int, byte[]> Data;

            public Track(int Cylinder, int Head, int Sectors, Dictionary<int, byte[]> Data)
            {
                this.Cylinder = Cylinder;
                this.Head = Head;
                this.Sectors = Sectors;
                this.Data = Data;
            }
        }
    }
}
