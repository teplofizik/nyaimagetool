using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types.Extensions
{
    class Ext4INode : ExtINode
    {
        /// <summary>
        /// Wrapper for INode struct
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        public Ext4INode(byte[] Data, long Offset) : base(Data, Offset, 0x100) // ext4 => 256 bytes
        {

        }

        /// <summary>
        /// Size of this inode - 128. Alternately, the size of the extended inode fields beyond the original ext2 inode, including this field.
        /// u16 i_extra_isize (0x80)
        /// </summary>
        public uint ExtraISize
        {
            get { return ReadUInt16(0x80); }
            set { WriteUInt16(0x80, value); }
        }

        /// <summary>
        /// Upper 16-bits of the inode checksum.
        /// u16 i_checksum_hi (0x82)
        /// </summary>
        public uint ChecksumHi
        {
            get { return ReadUInt16(0x82); }
            set { WriteUInt16(0x82, value); }
        }

        /// <summary>
        /// Extra change time bits. This provides sub-second precision. See Inode Timestamps section.
        /// u32 i_ctime_extra (0x84)
        /// </summary>
        public uint CTimeExtra
        {
            get { return ReadUInt32(0x84); }
            set { WriteUInt32(0x84, value); }
        }

        /// <summary>
        /// Extra modification time bits. This provides sub-second precision.
        /// u32 i_mtime_extra (0x88)
        /// </summary>
        public uint MTimeExtra
        {
            get { return ReadUInt32(0x88); }
            set { WriteUInt32(0x88, value); }
        }

        /// <summary>
        /// Extra access time bits. This provides sub-second precision.
        /// u32 i_atime_extra (0x8C)
        /// </summary>
        public uint ATimeExtra
        {
            get { return ReadUInt32(0x8C); }
            set { WriteUInt32(0x8C, value); }
        }

        /// <summary>
        /// File creation time, in seconds since the epoch.
        /// u32 i_crtime (0x90)
        /// </summary>
        public uint CrTime
        {
            get { return ReadUInt32(0x90); }
            set { WriteUInt32(0x90, value); }
        }

        /// <summary>
        /// Extra access time bits. This provides sub-second precision.
        /// u32 i_crtime_extra (0x94)
        /// </summary>
        public uint CrTimeExtra
        {
            get { return ReadUInt32(0x94); }
            set { WriteUInt32(0x94, value); }
        }

        /// <summary>
        /// Upper 32-bits for version number.
        /// u32 i_version_hi (0x98)
        /// </summary>
        public uint Version
        {
            get { return ReadUInt32(0x98); }
            set { WriteUInt32(0x98, value); }
        }

        /// <summary>
        /// Project ID.
        /// u32 i_projid (0x9C)
        /// </summary>
        public uint ProjectID
        {
            get { return ReadUInt32(0x9C); }
            set { WriteUInt32(0x9C, value); }
        }

    }
}
