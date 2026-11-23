using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// ImageDisk (.imd) parser. Converts track/sector records into a flat
    /// sector image. Uniform images are copied directly; mixed-sector tracks
    /// are packed into the largest sector size and padded.
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

            while (Pos < Data.Length)
            {
                if (Pos + 5 > Data.Length)
                    break;

                Pos++; // recording mode
                int Cylinder = Data[Pos++];
                int Head = Data[Pos++];
                int Sectors = Data[Pos++];
                int SizeCode = Data[Pos++];
                if ((Sectors <= 0) || (Sectors > MaxSectors))
                    return false;

                int[] Sizes;
                if (SizeCode == 0xFF)
                {
                    Sizes = new int[Sectors];
                }
                else
                {
                    int Size = 128 << SizeCode;
                    Sizes = new int[Sectors];
                    for (int I = 0; I < Sectors; I++)
                        Sizes[I] = Size;
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

            if ((TrackList.Count == 0) || (MaxCylinder < 0) || (MaxHead < 0))
                return false;

            int TargetSize = 0;
            foreach (var Record in TrackList)
                foreach (var Payload in Record.Data.Values)
                    TargetSize = Math.Max(TargetSize, Payload.Length);
            if (TargetSize <= 0)
                return false;

            int TargetSpt = 0;
            foreach (var Record in TrackList)
            {
                int Bytes = 0;
                foreach (var Payload in Record.Data.Values)
                    Bytes += Payload.Length;
                int Needed = (Bytes + TargetSize - 1) / TargetSize;
                TargetSpt = Math.Max(TargetSpt, Needed);
            }
            if (TargetSpt <= 0)
                return false;

            int TrackCount = MaxCylinder + 1;
            int SideCount = MaxHead + 1;
            long Total = (long)TrackCount * SideCount * TargetSpt * TargetSize;
            if ((Total <= 0) || (Total > 256L * 1024 * 1024))
                return false;

            var Raw = new byte[Total];
            Array.Fill(Raw, (byte)0xE5);
            foreach (var Record in TrackList)
            {
                long TrackOffset = ((long)Record.Cylinder * SideCount + Record.Head) *
                    TargetSpt * TargetSize;
                int Packed = 0;
                var Keys = new List<int>(Record.Data.Keys);
                Keys.Sort();
                foreach (var Id in Keys)
                {
                    var Payload = Record.Data[Id];
                    if (Payload.Length == TargetSize)
                    {
                        int Index = Id - 1;
                        if ((Index >= 0) && (Index < TargetSpt))
                            Array.Copy(Payload, 0, Raw, TrackOffset + (long)Index * TargetSize, TargetSize);
                    }
                    else
                    {
                        int Copy = Math.Min(Payload.Length, (int)(Total - TrackOffset - Packed));
                        if (Copy > 0)
                        {
                            Array.Copy(Payload, 0, Raw, TrackOffset + Packed, Copy);
                            Packed += Copy;
                        }
                    }
                }
            }

            Image = new ImdImage(TrackCount, SideCount, TargetSpt, TargetSize, Raw);
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
