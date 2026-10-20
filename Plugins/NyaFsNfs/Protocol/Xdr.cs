using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFsNfs.Protocol
{
    internal sealed class XdrException : Exception
    {
        public XdrException(string Message) : base(Message)
        {
        }
    }

    internal sealed class XdrReader
    {
        private readonly byte[] Data;
        private int Position;

        public XdrReader(byte[] Data, int Position = 0)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            this.Position = Position;
        }

        public int Remaining => Data.Length - Position;

        public byte ReadByte()
        {
            Need(1);
            return Data[Position++];
        }

        public uint ReadUInt32()
        {
            Need(4);
            uint Value = (uint)((Data[Position] << 24) | (Data[Position + 1] << 16) |
                                (Data[Position + 2] << 8) | Data[Position + 3]);
            Position += 4;
            return Value;
        }

        public ulong ReadUInt64()
        {
            ulong High = ReadUInt32();
            ulong Low = ReadUInt32();
            return (High << 32) | Low;
        }

        public bool ReadBool() => ReadUInt32() != 0;

        public byte[] ReadFixedOpaque(int Length)
        {
            Need(Length);
            var Result = new byte[Length];
            Array.Copy(Data, Position, Result, 0, Length);
            Position += Length;
            Skip(Alignment(Length));
            return Result;
        }

        public byte[] ReadOpaque(int Maximum = 16 * 1024 * 1024)
        {
            uint Length = ReadUInt32();
            if (Length > Maximum)
                throw new XdrException($"Opaque value is too long: {Length}");

            Need((int)Length);
            var Result = new byte[Length];
            Array.Copy(Data, Position, Result, 0, (int)Length);
            Position += (int)Length;
            Skip(Alignment((int)Length));
            return Result;
        }

        public string ReadString(int Maximum = 16 * 1024 * 1024) =>
            Encoding.UTF8.GetString(ReadOpaque(Maximum));

        public void Skip(int Length)
        {
            Need(Length);
            Position += Length;
        }

        private void Need(int Length)
        {
            if ((Length < 0) || (Position + Length > Data.Length))
                throw new XdrException("Unexpected end of XDR data");
        }

        private static int Alignment(int Length) => (4 - (Length & 3)) & 3;
    }

    internal sealed class XdrWriter
    {
        private readonly List<byte> Data = new List<byte>();

        public int Length => Data.Count;

        public void WriteUInt32(uint Value)
        {
            Data.Add((byte)(Value >> 24));
            Data.Add((byte)(Value >> 16));
            Data.Add((byte)(Value >> 8));
            Data.Add((byte)Value);
        }

        public void WriteInt32(int Value) => WriteUInt32(unchecked((uint)Value));

        public void WriteUInt64(ulong Value)
        {
            WriteUInt32((uint)(Value >> 32));
            WriteUInt32((uint)Value);
        }

        public void WriteBool(bool Value) => WriteUInt32(Value ? 1u : 0u);

        public void WriteFixedOpaque(byte[] Value)
        {
            Data.AddRange(Value ?? Array.Empty<byte>());
            WritePadding((Value?.Length ?? 0));
        }

        public void WriteOpaque(byte[] Value)
        {
            var Bytes = Value ?? Array.Empty<byte>();
            WriteUInt32((uint)Bytes.Length);
            Data.AddRange(Bytes);
            WritePadding(Bytes.Length);
        }

        public void WriteString(string Value) =>
            WriteOpaque(Encoding.UTF8.GetBytes(Value ?? ""));

        public void WriteRaw(byte[] Value) => Data.AddRange(Value ?? Array.Empty<byte>());

        public byte[] ToArray() => Data.ToArray();

        private void WritePadding(int Length)
        {
            int Padding = (4 - (Length & 3)) & 3;
            for (int i = 0; i < Padding; i++)
                Data.Add(0);
        }
    }
}
