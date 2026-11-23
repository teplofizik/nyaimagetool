using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsAncientFs
{
    /// <summary>
    /// WOZ1/WOZ2 Apple II 5.25" image reader. The track bitstream is decoded
    /// as 6-and-2 nibbles and written in ProDOS logical-sector order.
    /// </summary>
    public sealed class WozImage
    {
        private const int BlockSize = 512;
        private const int Tracks = 35;
        private const int SectorsPerTrack = 16;
        private const int SectorSize = 256;
        private const int NibblesPerTrack = 6656;

        private static readonly int[] PhysicalToLogical =
        {
            0, 8, 1, 9, 2, 10, 3, 11,
            4, 12, 5, 13, 6, 14, 7, 15
        };

        private static readonly byte[] Decode6x2 =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x01,
            0xFF, 0xFF, 0x02, 0x03, 0xFF, 0x04, 0x05, 0x06,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x07, 0x08,
            0xFF, 0xFF, 0xFF, 0x09, 0x0A, 0x0B, 0x0C, 0x0D,
            0xFF, 0xFF, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
            0xFF, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x1B, 0xFF, 0x1C, 0x1D, 0x1E,
            0xFF, 0xFF, 0xFF, 0x1F, 0xFF, 0xFF, 0x20, 0x21,
            0xFF, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x29, 0x2A, 0x2B,
            0xFF, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32,
            0xFF, 0xFF, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0xFF, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
        };

        public readonly byte[] ProDosImage;

        private WozImage(byte[] ProDosImage)
        {
            this.ProDosImage = ProDosImage;
        }

        public static bool TryRead(byte[] Data, out WozImage Image)
        {
            Image = null;
            if ((Data == null) || (Data.Length < 12))
                return false;
            if (!StartsWith(Data, "WOZ1") && !StartsWith(Data, "WOZ2"))
                return false;
            if ((Data[4] != 0xFF) || (Data[5] != 0x0A) || (Data[6] != 0x0D) || (Data[7] != 0x0A))
                return false;

            bool Woz2 = StartsWith(Data, "WOZ2");
            int Pos = 12;
            int DiskType = 0;
            int Sides = 1;
            byte[] Tmap = null;
            var Trks = new List<TrackRef>();
            bool SawInfo = false;

            while (Pos + 8 <= Data.Length)
            {
                string Id = Encoding.ASCII.GetString(Data, Pos, 4);
                int Size = ReadLe32(Data, Pos + 4);
                Pos += 8;
                if ((Size < 0) || (Pos + Size > Data.Length))
                    return false;

                if (Id == "INFO")
                {
                    if (Size < 60)
                        return false;
                    DiskType = Data[Pos + 1];
                    if ((Data[Pos] >= 2) && (Size >= 64))
                        Sides = Data[Pos + 40];
                    SawInfo = true;
                }
                else if (Id == "TMAP")
                {
                    if (Size != 160)
                        return false;
                    Tmap = new byte[160];
                    Array.Copy(Data, Pos, Tmap, 0, 160);
                }
                else if (Id == "TRKS")
                {
                    SawInfo = true;
                    if (Woz2)
                    {
                        int Count = Size / 8;
                        for (int I = 0; I < Count; I++)
                        {
                            int Block = ReadLe16(Data, Pos + I * 8);
                            int Length = ReadLe16(Data, Pos + I * 8 + 2);
                            int Bits = ReadLe32(Data, Pos + I * 8 + 4);
                            Trks.Add(new TrackRef(Block, Length, Bits));
                        }
                    }
                    else
                    {
                        for (int I = 0; Pos + I * 6656 + 6656 <= Pos + Size; I++)
                        {
                            int Offset = Pos + I * 6656;
                            int BytesUsed = Data[Offset + 6646] | (Data[Offset + 6647] << 8);
                            int Bits = Data[Offset + 6648] | (Data[Offset + 6649] << 8);
                            var Raw = new byte[6646];
                            Array.Copy(Data, Offset, Raw, 0, 6646);
                            Trks.Add(new TrackRef(-1, BytesUsed, Bits, Raw));
                        }
                    }
                }

                Pos += Size;
            }

            if (!SawInfo || (DiskType != 1) || (Sides != 1) || (Tmap == null) || (Trks.Count == 0))
                return false;

            var Output = new byte[Tracks * SectorsPerTrack * SectorSize];
            for (int Track = 0; Track < Tracks; Track++)
            {
                int Index = Tmap[Track * 4];
                if (Index == 0xFF || Index >= Trks.Count)
                    continue;

                var Ref = Trks[Index];
                byte[] TrackData;
                if (Woz2)
                {
                    if ((Ref.Start < 0) || (Ref.Length <= 0))
                        continue;
                    long Start = (long)Ref.Start * BlockSize;
                    long Length = (long)Ref.Length * BlockSize;
                    if ((Start < 0) || (Start + Length > Data.Length))
                        continue;
                    TrackData = new byte[Length];
                    Array.Copy(Data, Start, TrackData, 0, Length);
                }
                else
                {
                    TrackData = new byte[Ref.Raw.Length];
                    Array.Copy(Ref.Raw, TrackData, TrackData.Length);
                }

                DecodeTrack(TrackData, Ref.Bits, Track, Output);
            }

            Image = new WozImage(Output);
            return true;
        }

        private static void DecodeTrack(byte[] TrackData, int BitCount, int Track, byte[] Output)
        {
            if ((BitCount <= 0) || (BitCount > TrackData.Length * 8))
                return;

            var Bits = new StringBuilder(BitCount);
            for (int I = 0; I < BitCount; I++)
            {
                int Byte = TrackData[I / 8];
                Bits.Append(((Byte >> (7 - (I % 8))) & 1) != 0 ? '1' : '0');
            }

            DecodeBits(Bits.ToString(), Track, Output);
        }

        internal static void DecodeBits(string Data, int Track, byte[] Output)
        {
            string Sync = "1111111100";
            string SyncRun = Sync + Sync + Sync + Sync + Sync;
            int SyncPos = Data.IndexOf(SyncRun, StringComparison.Ordinal);
            if (SyncPos < 0)
            {
                Data = Data.Substring(Data.Length - Sync.Length) + Data.Substring(0, Data.Length - Sync.Length);
                SyncPos = Data.IndexOf(SyncRun, StringComparison.Ordinal);
            }
            if (SyncPos < 0)
                return;

            Data = Data.Substring(SyncPos + SyncRun.Length);

            var Nibbles = new List<byte>();
            for (int I = 0; I < Data.Length; I++)
            {
                if (Data[I] != '1')
                    continue;

                if (I + 7 >= Data.Length)
                    break;
                int Value = 0;
                for (int Bit = 0; Bit < 8; Bit++)
                    Value = (Value << 1) | (Data[I + Bit] == '1' ? 1 : 0);
                Nibbles.Add((byte)Value);
                I += 7;
            }

            int Pos = 0;
            while (Pos + 3 < Nibbles.Count)
            {
                int Address = Find(Nibbles, Pos, 0xD5, 0xAA, 0x96);
                if (Address < 0 || Address + 11 > Nibbles.Count)
                    break;

                int Volume = Decode4x4(Nibbles[Address + 3], Nibbles[Address + 4]);
                int ATrack = Decode4x4(Nibbles[Address + 5], Nibbles[Address + 6]);
                int Sector = Decode4x4(Nibbles[Address + 7], Nibbles[Address + 8]);
                int Checksum = Decode4x4(Nibbles[Address + 9], Nibbles[Address + 10]);
                if ((ATrack != Track) || (Checksum != (Volume ^ Track ^ Sector)))
                {
                    Pos = Address + 3;
                    continue;
                }

                int DataPos = Find(Nibbles, Address + 11, 0xD5, 0xAA, 0xAD);
                if ((DataPos < 0) || (DataPos - Address > 200) || (DataPos + 3 + 343 > Nibbles.Count))
                {
                    Pos = Address + 3;
                    continue;
                }

                var Bytes = DecodeSector(Nibbles, DataPos + 3);
                if (Bytes == null || Sector >= PhysicalToLogical.Length)
                {
                    Pos = DataPos + 3 + 343;
                    continue;
                }

                int Logical = PhysicalToLogical[Sector];
                Array.Copy(Bytes, 0, Output, (Track * SectorsPerTrack + Logical) * SectorSize, SectorSize);
                Pos = DataPos + 3 + 343;
            }
        }

        private static byte[] DecodeSector(List<byte> Nibbles, int Pos)
        {
            var Values = new int[343];
            for (int I = 0; I < 343; I++)
                Values[I] = Decode6x2[Nibbles[Pos + I] & 0x7F];

            for (int I = 1; I < 343; I++)
                Values[I] ^= Values[I - 1];

            if (Values[342] != 0)
                return null;

            var Bytes = new byte[SectorSize];
            int[] Flip = { 0, 2, 1, 3 };
            for (int I = 0; I < SectorSize; I++)
            {
                int High = Values[86 + I];
                int Low = Flip[(Values[I % 86] >> ((I / 86) * 2)) & 3];
                Bytes[I] = (byte)((High << 2) | Low);
            }
            return Bytes;
        }

        private static int Find(List<byte> Nibbles, int Start, byte A, byte B, byte C)
        {
            for (int I = Start; I + 2 < Nibbles.Count; I++)
            {
                if ((Nibbles[I] == A) && (Nibbles[I + 1] == B) && (Nibbles[I + 2] == C))
                    return I;
            }
            return -1;
        }

        private static int Decode4x4(int High, int Low)
        {
            int A = (High << 1) | 1;
            return A & Low;
        }

        private static byte[] BuildDecode6x2()
        {
            var Result = new byte[128];
            Array.Fill(Result, (byte)0xFF);
            int[] Valid =
            {
                0x96, 0x97, 0x9A, 0x9B, 0x9D, 0x9E, 0x9F,
                0xA6, 0xA7, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
                0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
                0xCB, 0xCD, 0xCE, 0xCF,
                0xD3, 0xD6, 0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
                0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
                0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
            };
            for (int I = 0; I < Valid.Length; I++)
                Result[Valid[I] & 0x7F] = (byte)I;
            return Result;
        }

        private static bool StartsWith(byte[] Data, string Text)
        {
            for (int I = 0; I < Text.Length; I++)
                if (Data[I] != (byte)Text[I])
                    return false;
            return true;
        }

        private static int ReadLe16(byte[] Data, int Offset) =>
            Data[Offset] | (Data[Offset + 1] << 8);

        private static int ReadLe32(byte[] Data, int Offset) =>
            Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24);

        private sealed class TrackRef
        {
            public readonly int Start;
            public readonly int Length;
            public readonly int Bits;
            public readonly byte[] Raw;

            public TrackRef(int Start, int Length, int Bits, byte[] Raw = null)
            {
                this.Start = Start;
                this.Length = Length;
                this.Bits = Bits;
                this.Raw = Raw;
            }
        }
    }
}
