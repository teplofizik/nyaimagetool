using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsWdNas.Types
{
    public sealed class WdNasImageBlob
    {
        public readonly string Name;
        public readonly int Offset;
        public readonly int Length;
        public readonly uint Checksum;
        public readonly byte[] Data;

        public WdNasImageBlob(string Name, int Offset, int Length, uint Checksum, byte[] Data)
        {
            this.Name = Name;
            this.Offset = Offset;
            this.Length = Length;
            this.Checksum = Checksum;
            this.Data = Data;
        }
    }

    public sealed class WdNasExtractionTarget
    {
        public readonly int HeaderOffset;
        public readonly string DirectoryName;
        public readonly string FileName;
        public readonly ushort Mode;
        public readonly byte Execute;
        public readonly int Offset;
        public readonly int Length;
        public readonly uint Checksum;
        public readonly int NextOffset;
        public readonly byte[] Header;
        public readonly byte[] Data;

        public WdNasExtractionTarget(int HeaderOffset, string DirectoryName, string FileName,
            ushort Mode, byte Execute, int Offset, int Length, uint Checksum, int NextOffset,
            byte[] Header, byte[] Data)
        {
            this.HeaderOffset = HeaderOffset;
            this.DirectoryName = DirectoryName;
            this.FileName = FileName;
            this.Mode = Mode;
            this.Execute = Execute;
            this.Offset = Offset;
            this.Length = Length;
            this.Checksum = Checksum;
            this.NextOffset = NextOffset;
            this.Header = Header;
            this.Data = Data;
        }

        public string Path => ((DirectoryName ?? "").TrimEnd('/') + "/" + FileName).TrimStart('/');
    }

    /// <summary>
    /// Western Digital My Cloud DL2100 firmware .bin container.
    /// Format reference: https://github.com/michaelroland/wdnas-firmware-modding
    /// </summary>
    public sealed class WdNasImage
    {
        public const int MainHeaderSize = 128;
        public const int CommonHeaderSize = 96;
        public const int CfsHeaderSize = 2048;

        private static readonly string[] ImageNames = {
            "uImage", "uRamdisk", "image.cfs", "config_default.tar.gz"
        };

        public readonly bool Correct;
        public readonly byte[] Raw;
        public readonly string Codename = "";
        public readonly string FirmwareVersion = "";
        public readonly uint HeaderChecksum;
        public readonly int NextOffset;
        public readonly WdNasImageBlob[] Images;
        public readonly WdNasExtractionTarget[] Targets;

        public WdNasImage(byte[] Data)
        {
            if ((Data == null) || (Data.Length < MainHeaderSize))
                return;

            Raw = Data;
            if ((Data[48] != 0x55) || (Data[49] != 0xAA) ||
                (Data[58] != 0x55) || (Data[59] != 0xAA))
                return;

            Codename = ReadString(Data, 50, 8);
            FirmwareVersion = ReadString(Data, 65, 47);
            HeaderChecksum = ReadU32(Data, 120);
            NextOffset = ToInt(ReadU32(Data, 124));

            var List = new List<WdNasImageBlob>();
            for (var I = 0; I < 4; I++)
            {
                var Offset = ToInt(ReadU32(Data, I * 8));
                var Length = ToInt(ReadU32(Data, I * 8 + 4));
                if ((Offset < MainHeaderSize) || (Length < 0) ||
                    ((long)Offset + Length > Data.Length))
                    return;

                List.Add(new WdNasImageBlob(ImageNames[I], Offset, Length,
                    ReadU32(Data, 32 + I * 4), Slice(Data, Offset, Length)));
            }
            Images = List.ToArray();

            var Extracts = new List<WdNasExtractionTarget>();
            var Position = NextOffset;
            var Seen = new HashSet<int>();
            while (Position != 0)
            {
                if ((Position < MainHeaderSize) || (Position + CommonHeaderSize > Data.Length) ||
                    !Seen.Add(Position))
                    return;

                var Length = ToInt(ReadU32(Data, Position + 84));
                var Offset = ToInt(ReadU32(Data, Position + 80));
                if ((Length < 0) || ((long)Offset + Length > Data.Length))
                    return;

                var Header = Slice(Data, Position, CommonHeaderSize);
                Extracts.Add(new WdNasExtractionTarget(
                    Position,
                    ReadString(Data, Position, 32),
                    ReadString(Data, Position + 32, 32),
                    (ushort)ReadU16(Data, Position + 64),
                    Data[Position + 66],
                    Offset, Length, ReadU32(Data, Position + 88),
                    ToInt(ReadU32(Data, Position + 92)), Header,
                    Slice(Data, Offset, Length)));

                Position = ToInt(ReadU32(Data, Position + 92));
            }
            Targets = Extracts.ToArray();
            Correct = true;
        }

        public byte[] Image(string Name)
        {
            foreach (var Image in Images)
                if (Image.Name == Name)
                    return Image.Data;
            return null;
        }

        public byte[] CfsSquashFs
        {
            get
            {
                var Cfs = Image("image.cfs");
                if ((Cfs == null) || (Cfs.Length < CfsHeaderSize))
                    return null;

                var Length = ToInt(ReadU32(Cfs, 0));
                if ((Length < 0) || (CfsHeaderSize + Length > Cfs.Length))
                    return null;
                return Slice(Cfs, CfsHeaderSize, Length);
            }
        }

        public uint HeaderChecksumCalculated
        {
            get
            {
                var Copy = (byte[])Raw.Clone();
                WriteU32(Copy, 120, 0);
                return XorChecksum(Copy, 0, MainHeaderSize);
            }
        }

        public static uint XorChecksum(byte[] Data, int Offset, int Length)
        {
            uint Result = 0;
            for (var I = 0; I < Length; I++)
                Result ^= (uint)Data[Offset + I] << ((I & 3) * 8);
            return Result;
        }

        private static uint ReadU16(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8));

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) |
                (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static int ToInt(uint Value) => (Value > int.MaxValue) ? -1 : (int)Value;

        private static string ReadString(byte[] Data, int Offset, int Length)
        {
            var End = Offset;
            var Limit = Math.Min(Offset + Length, Data.Length);
            while ((End < Limit) && (Data[End] != 0))
                End++;
            return Encoding.ASCII.GetString(Data, Offset, End - Offset);
        }

        private static byte[] Slice(byte[] Data, int Offset, int Length)
        {
            var Result = new byte[Length];
            Buffer.BlockCopy(Data, Offset, Result, 0, Length);
            return Result;
        }

        internal static void WriteU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }
    }
}
