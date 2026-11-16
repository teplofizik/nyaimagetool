using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTvip
{
    /// <summary>
    /// TVIP firmware container: a 0x24-byte global header (model/version) followed by
    /// sections. Each section is [name[7]][flags u32][size u32 BE][md5[16]] + data.
    /// The last section may be truncated in device dumps.
    /// </summary>
    public class TvipImage
    {
        public const int GlobalHeaderSize = 0x24;
        public const int SectionHeaderSize = 0x1F;

        public class Section
        {
            public string Name;
            public uint Flags;
            public uint Size;
            public byte[] Md5;
            public long HeaderOffset;
            public long DataOffset;
            public long AvailableLength;
            public bool Truncated => AvailableLength < Size;
        }

        private readonly byte[] Raw;

        public bool Correct { get; private set; }
        public string Model { get; private set; } = "";
        public string Version { get; private set; } = "";
        public byte[] GlobalHeader { get; private set; }
        public List<Section> Sections { get; } = new List<Section>();

        public TvipImage(byte[] Raw)
        {
            this.Raw = Raw;
            if ((Raw == null) || (Raw.Length < GlobalHeaderSize))
                return;

            if ((Raw[0] != 'T') || (Raw[1] != 'V') || (Raw[2] != 'I') || (Raw[3] != 'P'))
                return;

            GlobalHeader = ReadArray(0, GlobalHeaderSize);
            Model = ReadCString(0x18, 0x1F - 0x18);
            Version = ReadCString(0x1D, 0x24 - 0x1D);

            var Position = GlobalHeaderSize;
            while (Position + SectionHeaderSize <= Raw.Length)
            {
                var Name = ReadCString(Position, 7);
                if (!IsPrintable(Name))
                    break;

                var Flags = ReadU32BE(Position + 7);
                var Size = ReadU32BE(Position + 11);
                var Md5 = ReadArray(Position + 15, 16);

                var DataOffset = Position + SectionHeaderSize;
                var Available = Math.Min((long)Size, Raw.Length - DataOffset);
                if (Available < 0)
                    Available = 0;

                Sections.Add(new Section
                {
                    Name = Name,
                    Flags = Flags,
                    Size = Size,
                    Md5 = Md5,
                    HeaderOffset = Position,
                    DataOffset = DataOffset,
                    AvailableLength = Available
                });

                if ((Size == 0) || (DataOffset + Size > Raw.Length))
                    break;

                Position = (int)(DataOffset + Size);
            }

            Correct = Sections.Count > 0;
        }

        public byte[] GetData(Section Section) =>
            ReadArray(Section.DataOffset, Section.AvailableLength);

        public Section Find(string Name)
        {
            foreach (var Section in Sections)
                if (String.Equals(Section.Name, Name, StringComparison.OrdinalIgnoreCase))
                    return Section;
            return null;
        }

        private byte[] ReadArray(long Offset, long Length)
        {
            if ((Offset < 0) || (Length < 0) || (Offset + Length > Raw.Length))
                return new byte[0];

            var Result = new byte[(int)Length];
            Buffer.BlockCopy(Raw, (int)Offset, Result, 0, (int)Length);
            return Result;
        }

        private string ReadCString(long Offset, int MaxLength)
        {
            if ((Offset < 0) || (Offset >= Raw.Length))
                return "";

            var End = (int)Math.Min(Raw.Length, Offset + MaxLength);
            var Length = 0;
            while ((Offset + Length < End) && (Raw[Offset + Length] != 0))
                Length++;

            return Encoding.ASCII.GetString(Raw, (int)Offset, Length);
        }

        private static bool IsPrintable(string Value)
        {
            if (String.IsNullOrEmpty(Value))
                return false;

            foreach (var C in Value)
                if ((C < 0x20) || (C > 0x7E))
                    return false;

            return true;
        }

        private uint ReadU32BE(long Offset) =>
            (uint)((Raw[Offset] << 24) | (Raw[Offset + 1] << 16) | (Raw[Offset + 2] << 8) | Raw[Offset + 3]);
    }
}
