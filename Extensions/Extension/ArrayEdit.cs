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

        /// <summary>
        /// Calc padding for size
        /// </summary>
        /// <param name="Size">Size to be padded</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static long MakeSizeAligned(this long Size, long Align)
        {
            for (long i = 0; i < Align; i++)
            {
                if ((Size + i) % Align == 0)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Calc padding for size
        /// </summary>
        /// <param name="Size">Size to be padded</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static uint MakeSizeAligned(this uint Size, uint Align) => Convert.ToUInt32(MakeSizeAligned((long)Size, (long)Align));

        /// <summary>
        /// Calc padding for size
        /// </summary>
        /// <param name="Size">Size to be padded</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static int MakeSizeAligned(this int Size, int Align) => Convert.ToInt32(MakeSizeAligned((long)Size, (long)Align));

        /// <summary>
        /// Get aligned address (or padded size)
        /// </summary>
        /// <param name="Address">Address or size to be aligned</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static long GetAligned(this long Size, long Align) => Size + Size.MakeSizeAligned(Align);

        /// <summary>
        /// Get aligned address (or padded size)
        /// </summary>
        /// <param name="Address">Address or size to be aligned</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static uint GetAligned(this uint Size, uint Align) => Size + Size.MakeSizeAligned(Align);

        /// <summary>
        /// Get aligned address (or padded size)
        /// </summary>
        /// <param name="Address">Address or size to be aligned</param>
        /// <param name="Align">Alignment in bytes</param>
        /// <returns></returns>
        public static int GetAligned(this int Address, int Align) => Address + Address.MakeSizeAligned(Align);

        /// <summary>
        /// Fill array with specified value
        /// </summary>
        /// <typeparam name="T">Type of array</typeparam>
        /// <param name="Data">Array to be filled</param>
        /// <param name="Value">Value</param>
        static public void Fill<T>(this T[] Data, T Value)
        {
            if (Data == null) return;
            for (int i = 0; i < Data.Length; i++) Data[i] = Value;
        }

        /// <summary>
        /// Copy array data from one array to another
        /// </summary>
        /// <typeparam name="T">Type of array</typeparam>
        /// <param name="Data"></param>
        /// <param name="New"></param>
        static public void Copy<T>(this T[] Data, T[] New)
        {
            if (Data == null) return;
            if (New == null) return;

            int Length = Math.Min(New.Length, Data.Length);

            Buffer.BlockCopy(New, 0, Data, 0, Length);
        }

        /// <summary>
        /// Write ANSI null-terminated string to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Text">Content UTF8</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Offset after this field</returns>
        static public long WriteANSIString(this byte[] Data, long Offset, string Text, long Length)
        {
            byte[] TempData = ANSI.GetBytes(Text.ToCharArray());

            Data.WriteArray(Offset, TempData, Length);

            return Offset + Convert.ToUInt32(TempData.Length);
        }

        /// <summary>
        /// Read ANSI null-terminated string from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Readed string</returns>
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

        /// <summary>
        /// Write UTF8 null-terminated string to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Text">Content UTF8</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Offset after this field</returns>
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

        /// <summary>
        /// Read UTF8 null-terminated string from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Readed string</returns>
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

        /// <summary>
        /// Write byte array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Source">Data to be writed</param>
        /// <param name="Length">Length of data</param>
        /// <returns>Offset after this field</returns>
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

        /// <summary>
        /// Read byte array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of data to read</param>
        /// <returns>Readed array</returns>
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

        /// <summary>
        /// Read uint16 le array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint16[] value</returns>
        static public uint[] ReadUInt16Array(this byte[] Data, long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt16(Offset + i * 2);

            return Result;
        }

        /// <summary>
        /// Read uint16 be array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint16[] value</returns>
        static public uint[] ReadUInt16BEArray(this byte[] Data, long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt16BE(Offset + i * 2);

            return Result;
        }

        /// <summary>
        /// Read int16 le array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int16[] value</returns>
        static public int[] ReadInt16Array(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt32(Data.ReadUInt16(Offset + i * 2));

            return Result;
        }

        /// <summary>
        /// Read int16 be array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int16[] value</returns>
        static public int[] ReadInt16BEArray(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt32(Data.ReadUInt16BE(Offset + i * 2));

            return Result;
        }


        /// <summary>
        /// Read uint32 le array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint32[] value</returns>
        static public uint[] ReadUInt32Array(this byte[] Data, long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt32(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Read uint32 be array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint32[] value</returns>
        static public uint[] ReadUInt32BEArray(this byte[] Data, long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = Data.ReadUInt32BE(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Read int32 le array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int32[] value</returns>
        static public int[] ReadInt32Array(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = (int)Data.ReadUInt16(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Read int32 be array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int32[] value</returns>
        static public int[] ReadInt32BEArray(this byte[] Data, long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = (int)Data.ReadUInt32BE(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Read byte from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset to field</param>
        /// <returns>Readed byte value</returns>
        static public byte ReadByte(this byte[] Data, long Offset) => ((Offset < Data.Length) && (Offset >= 0)) ? Data[Offset] : (byte)0;

        /// <summary>
        /// Write byte to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset to field</param>
        /// <returns>Offset after byte</returns>
        static public long WriteByte(this byte[] Data, long Offset, byte Value)
        {
            if ((Data != null) && (Offset >= 0) && (Offset < Data.Length))
                Data[Offset + 0] = Convert.ToByte(Value & 0xFF); // LSB
            return Offset + 1;
        }

        /// <summary>
        /// Read uint16 le from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint16 value</returns>
        static public UInt16 ReadUInt16(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 0]) | // LSB
                    (Data[Offset + 1] << 8)); // MSB
        }

        /// <summary>
        /// Read uint16 le from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint16 value</returns>
        static public UInt16 ReadUInt16BE(this byte[] Data, long Offset)
        {
            if (Data == null) return 0;
            return Convert.ToUInt16((Data[Offset + 1]) | // MSB
                    (Data[Offset + 0] << 8)); // LSB
        }

        /// <summary>
        /// Write uint16 le to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint16 value</param>
        /// <returns>Offset after this field</returns>
        static public long WriteUInt16(this byte[] Data, long Offset, uint Value)
        {
            if ((Data != null) && (Offset >= 0) && (Offset < Data.Length - 1))
            {
                Data[Offset + 0] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        /// <summary>
        /// Write uint16 be to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint16 value</param>
        /// <returns>Offset after this field</returns>
        static public long WriteUInt16BE(this byte[] Data, long Offset, uint Value)
        {
            if (Data != null)
            {
                Data[Offset + 1] = Convert.ToByte(Value & 0xFF); // LSB
                Data[Offset + 0] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            }
            return Offset + 2;
        }

        /// <summary>
        /// Write uint32 le to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
        static public long WriteUInt32(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 0] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 3] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        /// <summary>
        /// Write uint32 be to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
        static public long WriteUInt32BE(this byte[] Data, long Offset, UInt32 Value)
        {
            Data[Offset + 3] = Convert.ToByte((Value >> 0) & 0xFF); // MSB
            Data[Offset + 2] = Convert.ToByte((Value >> 8) & 0xFF); // MSB
            Data[Offset + 1] = Convert.ToByte((Value >> 16) & 0xFF); // LSB
            Data[Offset + 0] = Convert.ToByte((Value >> 24) & 0xFF); // MSB

            return Offset + 4;
        }

        /// <summary>
        /// Read uint32 le from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint32 value</returns>
        static public UInt32 ReadUInt32(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 0]) |
                    ((uint)Data[Offset + 1] << 8) |
                    ((uint)Data[Offset + 2] << 16) | // MSB
                    ((uint)Data[Offset + 3] << 24)); // LSB
        }

        /// <summary>
        /// Read uint32 be from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint32 value</returns>
        static public UInt32 ReadUInt32BE(this byte[] Data, long Offset)
        {
            return (((uint)Data[Offset + 3]) |
                    ((uint)Data[Offset + 2] << 8) |
                    ((uint)Data[Offset + 1] << 16) | // MSB
                    ((uint)Data[Offset + 0] << 24)); // LSB
        }

        /// <summary>
        /// Write uint64 le to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
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

        /// <summary>
        /// Write uint64 be to array
        /// </summary>
        /// <param name="Data">Array to write to</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
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

        /// <summary>
        /// Read uint64 le from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint64 value</returns>
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

        /// <summary>
        /// Read uint64 be from array
        /// </summary>
        /// <param name="Data">Source array</param>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint64 value</returns>
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

    }
}
