using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Types
{
    class RmSuperblock : ArrayWrapper
    {
        public RmSuperblock() : base(0x20)
        {
            Magic = "-rom1fs-";
            VolumeName = "rootfs";
        }

        public RmSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x20)
        {

        }

        /// <summary>
        /// Magic 
        /// </summary>
        public string Magic
        {
            get { return ReadString(0x00, 0x08); }
            set { WriteString(0x00, value, 0x08); }
        }

        /// <summary>
        /// The number of accessible bytes in this fs.
        /// </summary>
        public uint FullSize
        {
            get { return ReadUInt32BE(0x08); }
            set { WriteUInt32BE(0x08, value); }
        }

        /// <summary>
        /// The checksum of the FIRST 512 BYTES.
        /// A simple sum of the longwords (assuming bigendian quantities again).
        /// </summary>
        public uint Checksum
        {
            get { return ReadUInt32BE(0x0C); }
            set { WriteUInt32BE(0x0C, value); }
        }

        private uint VolumeNameLength
        {
            get
            {
                for (uint i = 15; i < 256; i += 16)
                {
                    if (ReadByte(0x10 + i) == 0)
                        return i + 1;
                }
                return 16;
            }
        }

        /// <summary>
        /// Superblock size
        /// </summary>
        public uint SuperblockSize => 0x10 + VolumeNameLength;

        /// <summary>
        /// The zero terminated name of the volume, padded to 16 byte boundary.
        /// </summary>
        public string VolumeName
        {
            get { return ReadString(0x10, VolumeNameLength); }
            set { WriteString(0x10, value, VolumeNameLength); }
        }

    }
}
