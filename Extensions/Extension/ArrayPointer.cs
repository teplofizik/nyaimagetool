using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace Extension.Packet
{
    public class ArrayPointer
    {
        protected byte[] Raw;
        private long StructOffset = 0;
        private long StructSize = 0;

        /// <summary>
        /// Construct wrapper for parsing and editing Data array
        /// </summary>
        /// <param name="Data">Array to parsing and editing</param>
        /// <param name="Offset">Offset to struct</param>
        public ArrayPointer(byte[] Data, long Offset, long Size)
        {
            Raw = Data;
            StructOffset = Offset;
            StructSize = Size;
        }

        /// <summary>
        /// Go to next element (as c pointer)
        /// </summary>
        /// <param name="Steps"></param>
        public void Next(int Steps = 1)
        {
            StructOffset += StructSize * Steps;
        }

        /// <summary>
        /// Go to prev element (as c pointer)
        /// </summary>
        /// <param name="Steps"></param>
        public void Prev(int Steps = 1)
        {
            StructOffset -= StructSize * Steps;
        }

        public byte[] Data => Raw.ReadArray(StructOffset, StructSize);

        /// <summary>
        /// Определяет длину отдаваемого пакета
        /// </summary>
        /// <returns></returns>
        public virtual int getLength()
        {
            return Convert.ToInt32(StructSize);
        }

        /// <summary>
        /// Получить пакет
        /// </summary>
        /// <returns></returns>
        public virtual byte[] getPacket()
        {
            byte[] Result = new byte[getLength()];
            Buffer.BlockCopy(Raw, Convert.ToInt32(StructOffset), Result, 0, Result.Length);

            return Result;
        }

        /// <summary>
        /// Fill array with value
        /// </summary>
        /// <param name="Value">Value to write</param>
        public void Fill(byte Value)
        {
            for (long i = 0; i < StructSize; i++)
                WriteByte(StructOffset + i, Value);
        }

        // String access

        /// <summary>
        /// Write ANSI null-terminated string to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Text">Content</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Offset after this string</returns>
        public long WriteANSIString(long Offset, string Text, long Length) => Raw.WriteANSIString(StructOffset + Offset, Text, Length) - StructOffset;

        /// <summary>
        /// Read UTF8 null-terminated string from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of string</param>
        /// <returns>readed string</returns>
        public string ReadANSIString(long Offset, long Length) => Raw.ReadANSIString(StructOffset + Offset, Length);


        /// <summary>
        /// Write UTF8 null-terminated string to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Text">Content UTF8</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Offset after this field</returns>
        public long WriteString(long Offset, string Text, long Length) => Raw.WriteString(StructOffset + Offset, Text, Length) - StructOffset;

        /// <summary>
        /// Read ANSI null-terminated string from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of string</param>
        /// <returns>Readed string</returns>
        public string ReadString(long Offset, long Length) => Raw.ReadString(StructOffset + Offset, Length);

        // Byte access

        /// <summary>
        /// Write byte to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">Value of byte field</param>
        /// <returns>Offset after this field</returns>
        public long WriteByte(long Offset, long Value) => Raw.WriteByte(StructOffset + Offset, Convert.ToByte(Value & 0xFF)) - StructOffset;

        /// <summary>
        /// Read byte from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed byte value</returns>
        public byte ReadByte(long Offset) => Raw.ReadByte(StructOffset + Offset);

        /// <summary>
        /// Write uint16 le to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint16 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt16(long Offset, uint Value) => Raw.WriteUInt16(StructOffset + Offset, Convert.ToUInt16(Value & 0xFFFF)) - StructOffset;

        /// <summary>
        /// Write uint16 be to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint16 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt16BE(long Offset, uint Value) => Raw.WriteUInt16BE(StructOffset + Offset, Convert.ToUInt16(Value & 0xFFFF)) - StructOffset;

        /// <summary>
        /// Read uint16 le from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint16 value</returns>
        public UInt16 ReadUInt16(long Offset) => Raw.ReadUInt16(StructOffset + Offset);

        /// <summary>
        /// Read uint16 be from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint16 value</returns>
        public UInt16 ReadUInt1BE6(long Offset) => Raw.ReadUInt16BE(StructOffset + Offset);

        /// <summary>
        /// Write uint32 le to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt32(long Offset, uint Value) => Raw.WriteUInt32(StructOffset + Offset, Value) - StructOffset;

        /// <summary>
        /// Write uint32 be to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt32BE(long Offset, uint Value) => Raw.WriteUInt32BE(StructOffset + Offset, Value) - StructOffset;

        /// <summary>
        /// Read uint32 le from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint32 value</returns>
        public uint ReadUInt32(long Offset) => Raw.ReadUInt32(StructOffset + Offset);

        /// <summary>
        /// Read uint32 be from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint32 value</returns>
        public uint ReadUInt32BE(long Offset) => Raw.ReadUInt32BE(StructOffset + Offset);

        /// <summary>
        /// Write uint64 le to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint32 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt64(long Offset, ulong Value) => Raw.WriteUInt64(StructOffset + Offset, Value) - StructOffset;

        /// <summary>
        /// Write uint64 be to array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Value">uint64 value</param>
        /// <returns>Offset after this field</returns>
        public long WriteUInt64BE(long Offset, ulong Value) => Raw.WriteUInt64BE(StructOffset + Offset, Value) - StructOffset;

        /// <summary>
        /// Read uint64 le from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint64 value</returns>
        public ulong ReadUInt64(long Offset) => Raw.ReadUInt64(StructOffset + Offset);

        /// <summary>
        /// Read uint64 be from array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <returns>Readed uint64 value</returns>
        public ulong ReadUInt64BE(long Offset) => Raw.ReadUInt64BE(StructOffset + Offset);

        /// <summary>
        /// Write byte array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Data">Data to be writed</param>
        /// <param name="Length">Length of data</param>
        /// <returns>Offset after this field</returns>
        public long WriteArray(long Offset, byte[] Data, long Length) => Raw.WriteArray(StructOffset + Offset, Data, Length) - StructOffset;

        /// <summary>
        /// Read byte array
        /// </summary>
        /// <param name="Offset">Offset from start of struct to field</param>
        /// <param name="Length">Length of data to read</param>
        /// <returns>Readed array</returns>
        public byte[] ReadArray(long Offset, long Length) => Raw.ReadArray(StructOffset + Offset, Length);

        /// <summary>
        /// Read int16 le array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int16[] value</returns>
        public int[] ReadInt16Array(long Offset, long Length) => Raw.ReadInt16Array(StructOffset + Offset, Length);

        /// <summary>
        /// Read int16 be array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int16[] value</returns>
        public int[] ReadInt16BEArray(long Offset, long Length) => Raw.ReadInt16BEArray(StructOffset + Offset, Length);

        /// <summary>
        /// Read uint16 le array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint16[] value</returns>
        public uint[] ReadUInt16Array(long Offset, long Length) => Raw.ReadUInt16Array(StructOffset + Offset, Length);

        /// <summary>
        /// Read uint16 be array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint16[] value</returns>
        public uint[] ReadUInt16BEArray(long Offset, long Length) => Raw.ReadUInt16BEArray(StructOffset + Offset, Length);

        /// <summary>
        /// Read int32 le array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int32[] value</returns>
        public int[] ReadInt32Array(long Offset, long Length) => Raw.ReadInt32Array(StructOffset + Offset, Length);

        /// <summary>
        /// Read int32 be array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed int32[] value</returns>
        public int[] ReadInt32BEArray(long Offset, long Length) => Raw.ReadInt32BEArray(StructOffset + Offset, Length);

        /// <summary>
        /// Read uint32 le array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint32[] value</returns>
        public uint[] ReadUInt32Array(long Offset, long Length) => Raw.ReadUInt32Array(StructOffset + Offset, Length);

        /// <summary>
        /// Read uint32 be array
        /// </summary>
        /// <param name="Offset">Offset to field</param>
        /// <param name="Length">Lenght of readed array</param>
        /// <returns>Readed uint32[] value</returns>
        public uint[] ReadUInt32BEArray(long Offset, long Length) => Raw.ReadUInt32BEArray(StructOffset + Offset, Length);
    }
}
