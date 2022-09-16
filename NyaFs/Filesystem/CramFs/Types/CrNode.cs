using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Types
{
    class CrNode : ArrayWrapper
    {
        public CrNode(byte[] Data, long Offset) : base(Data, Offset, 0x0C)
        {

        }


        /// <summary>
        /// Mode (0, u16)
        /// </summary>
        public uint Mode
        {
            get { return ReadUInt16(0x00); }
            set { WriteUInt16(0x00, value); }
        }

        /// <summary>
        /// UID (2, u16)
        /// </summary>
        public uint UId
        {
            get { return ReadUInt16(0x02); }
            set { WriteUInt16(0x02, value); }
        }

        /// <summary>
        /// Size (4, u26)
        /// </summary>
        public uint Size
        {
            get { return ReadUInt32(0x04) & 0x3FFFFFFu; }
            set { WriteUInt32(0x04, (ReadUInt32(0x04) & 0xFC000000) | (value & 0x3FFFFFFu)); }
        }

        /// <summary>
        /// Gid (6, u6)
        /// </summary>
        public uint GId
        {
            get { return Convert.ToUInt32(ReadByte(0x07) >> 2) & 0x3Fu; }
            set { WriteByte(0x07, Convert.ToByte(ReadByte(0x07) & 0x03) | ((value & 0x3F) << 2)); }
        }

        /// <summary>
        /// NameLength
        /// </summary>
        public uint NameLen
        {
            get { return (ReadByte(0x08) & 0x3Fu) * 4; }
            set { WriteByte(0x08, Convert.ToByte(ReadByte(0x08) & 0xC0) | ((value / 4) & 0x3Fu)); }
        }

        /// <summary>
        /// Offset (8.6, u26)
        /// </summary>
        public uint Offset
        {
            get { return ((ReadUInt32(0x08) >> 6) & 0x3FFFFFFu) * 4; }
            set { WriteUInt32(0x08, (ReadUInt32(0x08) & 0x0000003F) | ((value / 4) & 0x3FFFFFFu) << 6); }
        }
    }
}
