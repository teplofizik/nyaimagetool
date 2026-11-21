using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Minimal StuffIt 1.x/3.x archive reader. It extracts data forks; resource
    /// forks and encrypted entries are not exposed. The LZ+Huffman (method 13)
    /// variant used by early Macintosh System disks is implemented.
    /// </summary>
    internal sealed class StuffItArchive
    {
        internal sealed class Item
        {
            public string Name;
            public byte[] Data;
        }

        private readonly byte[] Data;

        public StuffItArchive(byte[] Data)
        {
            this.Data = Data;
        }

        public static bool IsStuffIt(byte[] Data)
        {
            if ((Data == null) || (Data.Length < 22))
                return false;
            if (ReadBe32(Data, 0) != 0x53495421u) // "SIT!"
                return false;
            return ReadBe32(Data, 10) == 0x724C6175u; // "rLau"
        }

        public List<Item> ReadItems()
        {
            var Result = new List<Item>();
            uint TotalSize = ReadBe32(Data, 6);
            int End = (int)Math.Min(Data.Length, TotalSize);
            int Offset = 22;
            var Folders = new List<string>();

            while (Offset + 112 <= End)
            {
                var Header = new byte[112];
                Array.Copy(Data, Offset, Header, 0, 112);

                int NameLength = Math.Min((int)Header[2], 31);
                string Name = Decode(Header, 3, NameLength);
                int RMode = Header[0];
                int DMode = Header[1];
                uint RSize = ReadBe32(Header, 84);
                uint DSize = ReadBe32(Header, 88);
                uint RComp = ReadBe32(Header, 92);
                uint DComp = ReadBe32(Header, 96);

                int Start = Offset + 112;
                int RMethod = RMode & 0x6F;
                int DMethod = DMode & 0x6F;

                if ((RMethod == 0x20) || (DMethod == 0x20))
                {
                    Folders.Add(Name);
                }
                else if ((RMethod == 0x21) || (DMethod == 0x21))
                {
                    if (Folders.Count > 0)
                        Folders.RemoveAt(Folders.Count - 1);
                }
                else if ((DSize > 0) && ((DMode & 0x80) == 0))
                {
                    long CompressedOffset = Start + RComp;
                    if ((CompressedOffset >= 0) && (CompressedOffset + DComp <= Data.Length))
                    {
                        var Compressed = new byte[DComp];
                        Array.Copy(Data, CompressedOffset, Compressed, 0, (int)DComp);
                        var Output = Decompress(Compressed, DSize, DMode & 0x0F);
                        if (Output != null)
                        {
                            string Path = Name;
                            if (Folders.Count > 0)
                                Path = String.Join("/", Folders) + "/" + Name;
                            Result.Add(new Item { Name = Path, Data = Output });
                        }
                    }
                }

                Offset = checked(Start + (int)RComp + (int)DComp);
            }

            return Result;
        }

        private static byte[] Decompress(byte[] Input, uint OutputSize, int Method)
        {
            switch (Method)
            {
                case 0:
                    if (Input.Length >= OutputSize)
                    {
                        var Copy = new byte[OutputSize];
                        Array.Copy(Input, Copy, Copy.Length);
                        return Copy;
                    }
                    return null;
                case 13:
                    return new StuffIt13Decompressor(Input).Decompress((int)OutputSize);
                default:
                    return null;
            }
        }

        private static string Decode(byte[] Data, int Offset, int Length)
        {
            var Chars = new char[Length];
            for (int I = 0; I < Length; I++)
                Chars[I] = (char)Data[Offset + I];
            return new string(Chars).TrimEnd('\0');
        }

        internal static uint ReadBe32(byte[] Data, int Offset) =>
            ((uint)Data[Offset] << 24) | ((uint)Data[Offset + 1] << 16) |
            ((uint)Data[Offset + 2] << 8) | Data[Offset + 3];

    }
}
