using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Types
{
    class CrNode : ArrayWrapper
    {
        // private int SizeWidth = 24;

        public CrNode(uint Mode, uint Uid, uint Gid, uint Size, string Filename) : base(0x0C + Filename.Length.GetAligned(4))
        {
            this.Mode = Mode;
            this.Size = Size;
            UId = Uid;
            GId = Gid;

            NameLen = Convert.ToUInt32(Filename.Length);
            WriteString(0x0C, Filename, Filename.Length);
        }

        public CrNode(byte[] Data, long Offset) : base(Data, Offset, 0x0C)
        {
            Resize(NodeSize);
        }

        /// <summary>
        /// Node size
        /// </summary>
        public long NodeSize => 0x0C + NameLen;


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
        /// Size (4, u24)
        /// </summary>
        public uint Size
        {
            get { return ReadUInt32(0x04) & 0xFFFFFFu; }
            set { WriteUInt32(0x04, (ReadUInt32(0x04) & 0xFF000000) | (value & 0xFFFFFFu)); }
        }

        /// <summary>
        /// Gid (7, u8)
        /// </summary>
        public uint GId
        {
            get { return ReadByte(0x07); }
            set { WriteByte(0x07, Convert.ToByte(value & 0xFF)); }
        }

        /// <summary>
        /// NameLength
        /// </summary>
        public uint NameLen
        {
            get { return (ReadByte(0x08) & 0x3Fu) * 4; }
            set { WriteByte(0x08, Convert.ToByte(ReadByte(0x08) & 0xC0) | (((value + 3) / 4) & 0x3Fu)); }
        }

        /// <summary>
        /// Offset (8.6, u26)
        /// </summary>
        public uint Offset
        {
            get { return ((ReadUInt32(0x08) >> 6) & 0x3FFFFFFu) * 4; }
            set { WriteUInt32(0x08, (ReadUInt32(0x08) & 0x0000003F) | (((value + 3) / 4) & 0x3FFFFFFu) << 6); }
        }

        /// <summary>
        /// Filesystem node type
        /// </summary>
        public Universal.Types.FilesystemItemType FsNodeType => Universal.Helper.FsHelper.GetFsNodeType(Mode);

        /// <summary>
        /// Mode as string
        /// </summary>
        public string ModeStr => Universal.Helper.FsHelper.GetModeString(Mode);

        /// <summary>
        /// Filename
        /// </summary>
        public string Name => ReadString(0x0C, NameLen).Replace("\0", "");
    }
}
