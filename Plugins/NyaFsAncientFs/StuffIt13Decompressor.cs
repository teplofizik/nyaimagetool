using System;

namespace NyaFsAncientFs
{
    /// <summary>
    /// StuffIt 1.x method 13 ("LZ+Huffman") decoder. This is the compression
    /// used by the early Macintosh 400K System Disk archives.
    /// </summary>
    internal sealed class StuffIt13Decompressor
    {
        private sealed class BitReader
        {
            private readonly byte[] Data;
            private int BitPosition;

            public BitReader(byte[] Data) => this.Data = Data;

            public int ReadBit()
            {
                if ((BitPosition >> 3) >= Data.Length)
                    return 0;
                int Result = (Data[BitPosition >> 3] >> (BitPosition & 7)) & 1;
                BitPosition++;
                return Result;
            }

            public void Skip(int Count) => BitPosition += Count;

            public uint ReadBits(int Count)
            {
                uint Result = 0;
                for (int I = 0; I < Count; I++)
                    Result |= (uint)ReadBit() << I;
                return Result;
            }
        }

        private sealed class Huffman
        {
            private readonly int[] Left;
            private readonly int[] Right;
            private readonly int[] Value;
            private int Nodes;

            public Huffman(int[] Lengths)
            {
                int Max = 0;
                for (int I = 0; I < Lengths.Length; I++)
                    if (Lengths[I] > Max)
                        Max = Lengths[I];

                int Capacity = Math.Max(2, Lengths.Length * 32 + 2);
                Left = new int[Capacity];
                Right = new int[Capacity];
                Value = new int[Capacity];
                for (int I = 0; I < Capacity; I++)
                {
                    Left[I] = -1;
                    Right[I] = -1;
                    Value[I] = -1;
                }
                Nodes = 1;

                int Code = 0;
                for (int Length = 1; Length <= Max; Length++)
                {
                    for (int Symbol = 0; Symbol < Lengths.Length; Symbol++)
                    {
                        if (Lengths[Symbol] != Length)
                            continue;

                        Insert(Code, Length, Symbol);
                        Code++;
                    }
                    Code <<= 1;
                }
            }


            private void Insert(int Code, int Length, int Symbol)
            {
                int Node = 0;
                for (int Bit = Length - 1; Bit >= 0; Bit--)
                {
                    int B = (Code >> Bit) & 1;
                    ref int Next = ref (B == 0 ? ref Left[Node] : ref Right[Node]);
                    if (Next < 0)
                    {
                        Next = Nodes++;
                    }
                    Node = Next;
                }
                Value[Node] = Symbol;
            }

            public int Read(BitReader Reader)
            {
                int Node = 0;
                while (Value[Node] < 0)
                {
                    int Bit = Reader.ReadBit();
                    Node = (Bit == 0) ? Left[Node] : Right[Node];
                    if (Node < 0)
                        return -1;
                }
                return Value[Node];
            }
        }

        private sealed class MetaHuffman
        {
            private readonly int[] Codes = new int[37];
            private readonly int[] Lengths = new int[37];

            public MetaHuffman()
            {
                var LengthTable = new[]
                {
                    11,8,8,8,8,7,6,5,5,5,5,6,5,6,7,7,9,12,10,11,11,12,
                    12,11,11,11,12,12,12,12,12,5,2,2,3,4,5
                };
                var CodeTable = new[]
                {
                    0x5d8,0x058,0x040,0x0c0,0x000,0x078,0x02b,0x014,
                    0x00c,0x01c,0x01b,0x00b,0x010,0x020,0x038,0x018,
                    0x0d8,0xbd8,0x180,0x680,0x380,0xf80,0x780,0x480,
                    0x080,0x280,0x3d8,0xfd8,0x7d8,0x9d8,0x1d8,0x004,
                    0x001,0x002,0x007,0x003,0x008
                };

                for (int I = 0; I < 37; I++)
                {
                    Lengths[I] = LengthTable[I];
                    Codes[I] = CodeTable[I];
                }
            }

            public int Read(BitReader Reader)
            {
                int Code = 0;
                for (int Length = 1; Length <= 12; Length++)
                {
                    Code |= Reader.ReadBit() << (Length - 1);
                    for (int I = 0; I < 37; I++)
                        if ((Lengths[I] == Length) && (Codes[I] == Code))
                            return I;
                }
                return -1;
            }

            private static int Reverse(int Value, int Length)
            {
                int Result = 0;
                for (int I = 0; I < Length; I++)
                {
                    Result = (Result << 1) | (Value & 1);
                    Value >>= 1;
                }
                return Result;
            }
        }
        private readonly byte[] Input;
        private readonly BitReader Bits;

        public StuffIt13Decompressor(byte[] Input)
        {
            this.Input = Input;
            Bits = new BitReader(Input);
        }

        public byte[] Decompress(int OutputSize)
        {
            var Result = new byte[OutputSize];
            var Window = new byte[65536];
            int WindowPos = 0;
            int OutputPos = 0;

            int Val = ReadByte();
            Bits.Skip(8);
            int Code = Val >> 4;
            Huffman First;
            Huffman Second;
            Huffman Offset;

            if (Code == 0)
            {
                var Meta = new MetaHuffman();
                First = ParseCode(321, Meta);
                Second = (Val & 0x08) != 0 ? First : ParseCode(321, Meta);
                Offset = ParseCode((Val & 0x07) + 10, Meta);
            }
            else
            {
                // Static method-13 tables cover the other SIT! variants. The
                // early System disks use the dynamic form above.
                return null;
            }

            var Current = First;
            while (OutputPos < OutputSize)
            {
                int Symbol = Current.Read(Bits);
                if (Symbol < 0)
                    break;

                if (Symbol < 0x100)
                {
                    Current = First;
                    Window[WindowPos] = (byte)Symbol;
                    WindowPos = (WindowPos + 1) & 0xFFFF;
                    Result[OutputPos++] = (byte)Symbol;
                }
                else
                {
                    Current = Second;
                    int Length;
                    if (Symbol < 0x13E)
                        Length = Symbol - 0x100 + 3;
                    else if (Symbol == 0x13E)
                        Length = (int)Bits.ReadBits(10) + 65;
                    else if (Symbol == 0x13F)
                        Length = (int)Bits.ReadBits(15) + 65;
                    else
                        break;

                    int BitLength = Offset.Read(Bits);
                    int Distance;
                    if (BitLength == 0)
                        Distance = 1;
                    else if (BitLength == 1)
                        Distance = 2;
                    else
                        Distance = (1 << (BitLength - 1)) + (int)Bits.ReadBits(BitLength - 1) + 1;

                    int Source = (WindowPos - Distance) & 0xFFFF;
                    for (int I = 0; I < Length && OutputPos < OutputSize; I++)
                    {
                        byte B = Window[Source];
                        Source = (Source + 1) & 0xFFFF;
                        Window[WindowPos] = B;
                        WindowPos = (WindowPos + 1) & 0xFFFF;
                        Result[OutputPos++] = B;
                    }
                }
            }

            return Result;
        }

        private Huffman ParseCode(int Count, MetaHuffman Meta)
        {
            var Lengths = new int[Count];
            int Length = 0;

            for (int I = 0; I < Count; I++)
            {
                int Val = Meta.Read(Bits);
                switch (Val)
                {
                    case 31:
                        Length = -1;
                        break;
                    case 32:
                        Length++;
                        break;
                    case 33:
                        Length--;
                        break;
                    case 34:
                        if (Bits.ReadBit() != 0)
                        {
                            if (I < Count) Lengths[I] = Length;
                            I++;
                        }
                        break;
                    case 35:
                        {
                            int Run = (int)Bits.ReadBits(3) + 2;
                            while ((Run-- > 0) && (I < Count))
                                Lengths[I++] = Length;
                            break;
                        }
                    case 36:
                        {
                            int Run = (int)Bits.ReadBits(6) + 10;
                            while ((Run-- > 0) && (I < Count))
                                Lengths[I++] = Length;
                            break;
                        }
                    default:
                        Length = Val + 1;
                        break;
                }

                if (I < Count)
                    Lengths[I] = Length;
            }

            return new Huffman(Lengths);
        }

        private int ReadByte()
        {
            // Input bits are consumed only by the LZSS stream after the first
            // byte; the first byte is always byte-aligned at offset zero.
            return Input.Length > 0 ? Input[0] : 0;
        }

        private static int Reverse(int Value, int Length)
        {
            int Result = 0;
            for (int I = 0; I < Length; I++)
            {
                Result = (Result << 1) | (Value & 1);
                Value >>= 1;
            }
            return Result;
        }
    }
}
