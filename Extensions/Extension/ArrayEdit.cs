using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extension.Array
{
    public static class ArrayEdit
    {
        static Encoding ANSI = Encoding.GetEncoding(1251);

        public static long MakeSizeAligned(this long Size, long Align)
        {
            for (long i = 0; i < Align; i++)
            {
                if ((Size + i) % Align == 0)
                    return i;
            }
            return 0;
        }

        public static uint MakeSizeAligned(this uint Size, uint Align) => Convert.ToUInt32(MakeSizeAligned((long)Size, (long)Align));
        public static int MakeSizeAligned(this int Size, int Align) => Convert.ToInt32(MakeSizeAligned((long)Size, (long)Align));

        public static long GetAligned(this long Size, long Align) => Size + Size.MakeSizeAligned(Align);

        public static uint GetAligned(this uint Size, uint Align) => Size + Size.MakeSizeAligned(Align);
        public static int GetAligned(this int Size, int Align) => Size + Size.MakeSizeAligned(Align);

        static public void Fill<T>(this T[] Data, T Value)
        {
            if (Data == null) return;
            for (int i = 0; i < Data.Length; i++) Data[i] = Value;
        }

        static public void Copy<T>(this T[] Data, T[] New)
        {
            if (Data == null) return;
            if (New == null) return;

            int Length = Math.Min(New.Length, Data.Length);

            Buffer.BlockCopy(New, 0, Data, 0, Length);
        }


        /// <summary>
        /// Запись ANSI строки в массив байт
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        /// <param name="Text"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public long WriteANSIString(this byte[] Data, long Offset, string Text, long Length)
        {
            byte[] TempData = ANSI.GetBytes(Text.ToCharArray());

            Data.WriteArray(Offset, TempData, Length);

            return Offset + Convert.ToUInt32(TempData.Length);
        }

        /// <summary>
        /// Чтение ANSI строки из массива байт
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        static public string ReadANSIString(this byte[] Data, long Offset, long Length)
        {
            string Temp = "";

            byte[] TempArray = new byte[Length];
            Buffer.BlockCopy(Data, Convert.ToInt32(Offset), TempArray, 0, Convert.ToInt32(Length));

            Temp = ANSI.GetString(TempArray);

            int End = Temp.IndexOf('\0');
            if (End >= 0) Temp = Temp.Substring(0, End);

            return Temp;
        }

        static public long WriteString(this byte[] Data, long Offset, string Text, long Length)
        {
            int i;

            for (i = 0; i < Length; i++)
            {
                if (i < Text.Length)
                {
                    Data[Offset + i] = Convert.ToByte(Text[i]);
                }
                else
                {
                    Data[Offset + i] = 0;
                }
            }

            // Пследний символ всегда 0
            Data[Offset + Length - 1] = 0;

            return Offset + Length;
        }

        static public string ReadString(this byte[] Data, long Offset, long Length)
        {
            string Temp = "";

            for (int i = 0; i < Length; i++)
            {
                byte Char = Data[Offset + i];
                if (Char == 0) break;

                Temp += Convert.ToChar(Char);
            }

            return Temp;
        }

        static public long WriteArray(this byte[] Data, long Offset, byte[] Source, long Length)
        {
            int i;

            for (i = 0; i < Length; i++)
            {
                if (i < Source.Length)
                {
                    Data[Offset + i] = Source[i];
                }
                else
                {
                    Data[Offset + i] = 0;
                }
            }

            return Offset + Length;
        }

        static public byte[] ReadArray(this byte[] Data, long Offset, long Length)
        {
            long i;
            byte[] Res = new byte[Length];

            for (i = 0; i < Length; i++)
            {
                if (i < Data.Length)
                    Res[i] = Data[Offset + i] ;
                else
                    Res[i] = 0;
            }

            return Res;
        }

        static public byte[] ReadArrayFrom(this byte[] Data, long Offset)
        {
            long i;
            byte[] Res = new byte[Data.Length - Offset];

            for (i = 0; i < Res.Length; i++)
            {
                if (i < Data.Length)
                    Res[i] = Data[Offset + i];
                else
                    Res[i] = 0;
            }

            return Res;
        }

        // LB HB
        static public long WriteUInt16(this byte[] Data, long Offset, uint Value)
        {
            if (Data != null)
            {
                Data[Offset + 0] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        // LB HB
        static public UInt16 ReadUInt16(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 0]) | // LSB
                    (Data[Offset + 1] << 8)); // MSB
        }

        // HB LB
        static public long WriteUInt16BE(this byte[] Data, long Offset, uint Value)
        {
            if (Data != null)
            {
                Data[Offset + 1] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 0] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        // LB HB
        static public UInt16 ReadUInt16BE(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 1]) | // MSB
                    (Data[Offset + 0] << 8)); // LSB
        }

        // HB LB
        static public long WriteUInt32(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        // LB HB
        static public UInt32 ReadUInt32(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 0]) |
                    ((uint)Data[Offset + 1] << 8) |
                    ((uint)Data[Offset + 2] << 16) | // MSB
                    ((uint)Data[Offset + 3] << 24)); // LSB
        }

        // HB LB
        static public long WriteUInt32BE(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 3] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 0] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        // LB HB
        static public UInt32 ReadUInt32BE(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 3]) |
                    ((uint)Data[Offset + 2] << 8) |
                    ((uint)Data[Offset + 1] << 16) | // MSB
                    ((uint)Data[Offset + 0] << 24)); // LSB
        }

        // LB HB
        static public UInt64 ReadUInt64BE(this byte[] Data, long Offset)
        {
            return (((ulong)Data[Offset + 7]) |
                    ((ulong)Data[Offset + 6] << 8) |
                    ((ulong)Data[Offset + 5] << 16) | // MSB
                    ((ulong)Data[Offset + 4] << 24) |
                    ((ulong)Data[Offset + 3] << 32) |
                    ((ulong)Data[Offset + 2] << 40) |
                    ((ulong)Data[Offset + 1] << 48) |
                    ((ulong)Data[Offset + 0] << 56)); // LSB
        }


        // LB HB
        static public UInt64 ReadUInt64(this byte[] Data, long Offset)
        {
            return (((ulong)Data[Offset + 0]) |
                    ((ulong)Data[Offset + 1] << 8) |
                    ((ulong)Data[Offset + 2] << 16) | // MSB
                    ((ulong)Data[Offset + 3] << 24) |
                    ((ulong)Data[Offset + 4] << 32) |
                    ((ulong)Data[Offset + 5] << 40) |
                    ((ulong)Data[Offset + 6] << 48) |
                    ((ulong)Data[Offset + 7] << 56)); // LSB
        }

        // HB LB
        static public long WriteUInt64(this byte[] Data, long Offset, UInt64 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 24) & 0xFF); // MSB
            Data[Offset + 4] = Convert.ToByte((Value >> 32) & 0xFF); // MSB
            Data[Offset + 5] = Convert.ToByte((Value >> 40) & 0xFF); // MSB
            Data[Offset + 6] = Convert.ToByte((Value >> 48) & 0xFF); // MSB
            Data[Offset + 7] = Convert.ToByte((Value >> 56) & 0xFF); // MSB

            return Offset + 8;
        }

        // HB LB
        static public long WriteUInt64BE(this byte[] Data, long Offset, UInt64 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 56) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 48) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 40) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 32) & 0xFF); // MSB
            Data[Offset + 4] = Convert.ToByte((Value >> 24) & 0xFF); // MSB
            Data[Offset + 5] = Convert.ToByte((Value >> 16) & 0xFF); // MSB
            Data[Offset + 6] = Convert.ToByte((Value >> 8) & 0xFF);  // MSB
            Data[Offset + 7] = Convert.ToByte((Value >> 0) & 0xFF);  // MSB

            return Offset + 8;
        }

        static public long WriteByte(this byte[] Data, long Offset, byte Value)
        {
            Data[Offset] = Value;

            return Offset + 1;
        }
    }
}
