using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsRockchip.Types
{
    internal class RkfwHeader : NyaIO.Data.ArrayWrapper
    {
        public RkfwHeader(byte[] Raw) : base(Raw, 0, Raw.ReadUInt32(4))
        {
        }

        public uint Magic => ReadUInt32(0x00);

        public uint HeaderSize => ReadUInt16(0x04);

        public uint Version => ReadUInt32(0x06);
        public uint Code => ReadUInt32(0x0A);

        private int Year => ReadUInt16(0x0D);
        private int Month => ReadByte(0x10);
        private int Day => ReadByte(0x11);
        private int Hour => ReadByte(0x12);
        private int Minute => ReadByte(0x13);
        private int Seconds => ReadByte(0x14);
        public uint ChipID => ReadUInt32(0x15);
        public uint LoaderOffset => ReadUInt32(0x19);
        public uint LoaderLength => ReadUInt32(0x1D);
        public uint DataOffset => ReadUInt32(0x21);
        public uint DataLength => ReadUInt32(0x25);

        public uint Unknown_1 => ReadUInt32(0x29);

        /// <summary>
        /// 0x00000000 - просто update.img, 0x00000001 - если RKAF
        /// </summary>
        public uint RKFWtype => ReadUInt32(0x2D);

        public uint SysFStype => ReadUInt32(0x31);
        public uint BackupEnd => ReadUInt32(0x35);

        public DateTime Timestamp => new DateTime(Year, Month, Day, Hour, Minute, Seconds);

    }
}
