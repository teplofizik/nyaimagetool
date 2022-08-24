using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Extension.Array;

namespace Extension.Packet
{
    public class RawPacket
    {
        private byte[] Raw;

        /// <summary>
        /// Конструктор пакета по образцу
        /// </summary>
        /// <param name="Data"></param>
        public RawPacket(byte[] Data)
        {
            Raw = new byte[Data.Length];
            Buffer.BlockCopy(Data, 0, Raw, 0, Data.Length);
        }

        /// <summary>
        /// Конструктор пакета с указанием длины
        /// </summary>
        /// <param name="Length"></param>
        public RawPacket(long Length)
        {
            Raw = new byte[Length];
            Raw.Fill((byte)0);
        }

        /// <summary>
        /// Определяет длину отдаваемого пакета
        /// </summary>
        /// <returns></returns>
        public virtual int getLength()
        {
            return Raw.Length;
        }

        /// <summary>
        /// Получить пакет
        /// </summary>
        /// <returns></returns>
        public virtual byte[] getPacket()
        {
            byte[] Result = new byte[getLength()];
            Buffer.BlockCopy(Raw, 0, Result, 0, Result.Length);

            return Result;
        }

        /// <summary>
        /// Записать строку
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Text"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected long WriteANSIString(long Offset, string Text, int Length)
        {
            return Raw.WriteANSIString(Convert.ToUInt32(Offset), Text, Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Прочитать строку
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected string ReadANSIString(long Offset, int Length)
        {
            return Raw.ReadANSIString(Convert.ToUInt32(Offset), Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Записать строку
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Text"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected long WriteString(long Offset, string Text, int Length)
        {
            return Raw.WriteString(Convert.ToUInt32(Offset), Text, Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Прочитать строку
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected string ReadString(long Offset, int Length)
        {
            return Raw.ReadString(Convert.ToUInt32(Offset), Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Записать байт
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteByte(long Offset, int Value)
        {
            return Raw.WriteByte(Convert.ToUInt32(Offset), Convert.ToByte(Value & 0xFF));
        }

        /// <summary>
        /// Прочитать байт
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected byte ReadByte(long Offset)
        {
            return ((Offset < Raw.Length) && (Offset >= 0)) ? Raw[Offset] : (byte)0;
        }

        /// <summary>
        /// Записать uint16
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt16(long Offset, int Value)
        {
            return Raw.WriteUInt16(Convert.ToUInt32(Offset), Convert.ToUInt16(Value & 0xFFFF));
        }

        /// <summary>
        /// Записать uint16
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt16(long Offset, uint Value)
        {
            return WriteUInt16(Offset, Convert.ToInt32(Value));
        }

        /// <summary>
        /// Прочитать uint16
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected UInt16 ReadUInt16(long Offset)
        {
            return Raw.ReadUInt16(Offset);
        }

        /// <summary>
        /// Записать uint16 BE
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt16BE(long Offset, int Value)
        {
            return Raw.WriteUInt16BE(Convert.ToUInt32(Offset), Convert.ToUInt16(Value & 0xFFFF));
        }

        /// <summary>
        /// Записать uint16 BE
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt16BE(long Offset, uint Value)
        {
            return WriteUInt16BE(Offset, Convert.ToInt32(Value));
        }

        /// <summary>
        /// Прочитать uint16 be
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected UInt16 ReadUInt16BE(long Offset)
        {
            return Raw.ReadUInt16BE(Offset);
        }

        /// <summary>
        /// Записать uint32
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt32(long Offset, uint Value)
        {
            return Raw.WriteUInt32(Convert.ToUInt32(Offset), Value);
        }

        /// <summary>
        /// Прочитать uint32
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected uint ReadUInt32(long Offset)
        {
            return Raw.ReadUInt32(Offset);
        }

        /// <summary>
        /// Записать uint32 BE
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        protected long WriteUInt32BE(long Offset, uint Value)
        {
            return Raw.WriteUInt32BE(Convert.ToUInt32(Offset), Value);
        }

        /// <summary>
        /// Прочитать uint32 be
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected uint ReadUInt32BE(long Offset)
        {
            return Raw.ReadUInt32BE(Offset);
        }

        /// <summary>
        /// Прочитать uint64
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected ulong ReadUInt64(long Offset)
        {
            return Raw.ReadUInt64(Offset);
        }

        /// <summary>
        /// Прочитать uint64 BE
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected ulong ReadUInt64BE(long Offset)
        {
            return Raw.ReadUInt64BE(Offset);
        }

        /// <summary>
        /// Записать uint64
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected long WriteUInt64(long Offset, ulong Value)
        {
            return Raw.WriteUInt64(Offset, Value);
        }

        /// <summary>
        /// Записать uint64 be
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        protected long WriteUInt64BE(long Offset, ulong Value)
        {
            return Raw.WriteUInt64BE(Offset, Value);
        }


        /// <summary>
        /// Записать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Data"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected long WriteArray(long Offset, byte[] Data, long Length)
        {
            return Raw.WriteArray(Convert.ToUInt32(Offset), Data, Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Записать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Data"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected long WriteArray(long Offset, byte[] Data, long Length, long SourceOffset)
        {
            return Raw.WriteArray(Convert.ToUInt32(Offset), Data, Convert.ToUInt32(Length));
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected byte[] ReadArray(long Offset, long Length)
        {
            byte[] Result = new byte[Length];
            Result.Fill((byte)0);

            long ResLength = (Raw.Length - Offset >= Length) ? Length : Length - Offset;
            if (ResLength > 0)
                Buffer.BlockCopy(Raw, Convert.ToInt32(Offset), Result, 0, Convert.ToInt32(ResLength));

            return Result;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected uint[] ReadUInt32Array(long Offset, long Length)
        {
            uint[] Result = new uint[Length];
            Result.Fill(0u);

            for (int i = 0; i < Length; i++)
                Result[i] = ReadUInt32(Offset + i * 4);

            return Result;
        }

        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected int[] ReadInt32Array(long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt32(ReadUInt32(Offset + i * 4));

            return Result;
        }
        /// <summary>
        /// Прочитать массив
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        protected int[] ReadInt16Array(long Offset, long Length)
        {
            int[] Result = new int[Length];
            Result.Fill(0);

            for (int i = 0; i < Length; i++)
                Result[i] = Convert.ToInt32(ReadUInt16(Offset + i * 2));

            return Result;
        }
    }
}
