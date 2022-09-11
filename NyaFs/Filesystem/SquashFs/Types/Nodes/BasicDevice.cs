using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types.Nodes
{
    class BasicDevice : SqInode
    {
        public BasicDevice(uint Mode, uint User, uint Group, uint Major, uint Minor) : base(new byte[0x18], 0x18)
        {
            InodeType = SqInodeType.BasicBlockDevice;
            Permissions = Mode;
            GidIndex = Group;
            UidIndex = User;

            HardLinkCount = 1;

            uint DevMajor = (Major & 0xfff) << 8;
            uint DevMinor = (Minor & 0xff) | (Minor & 0xfff00) << 12;

            Device = DevMajor | DevMinor;
        }

        public BasicDevice(byte[] Data) : base(Data, 0x18)
        {

        }

        /// <summary>
        /// INode size
        /// </summary>
        internal override long INodeSize => 0x18; // TODO: add blocks_sizes calculation...

        /// <summary>
        /// The number of hard links to this device
        /// u32 hard_link_count (0x10)
        /// </summary>
        public uint HardLinkCount
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// To extract the major device number, (device & 0xfff00) >> 8. To extract the minor device number, use (dev & 0xff) | ((dev >> 12) & 0xfff00)
        /// u32 device (0x14)
        /// </summary>
        public uint Device
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Major device number
        /// </summary>
        public uint Major => (Device & 0xfff00) >> 8;

        /// <summary>
        /// Minor device number
        /// </summary>
        public uint Minor => (Device & 0xff) | ((Device >> 12) & 0xfff00);
    }
}
