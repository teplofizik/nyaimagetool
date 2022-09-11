using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    class SqDirectoryHeader : ArrayWrapper
    {
        public SqDirectoryHeader(uint Count, uint Start, uint INode) : base(new byte[0x0c], 0, 0x0C)
        {
            this.Count = Count - 1;
            this.Start = Start;
            this.INodeNumber = INode;
        }

        public SqDirectoryHeader(byte[] Data, long Offset) : base(Data, Offset, 0x0C)
        {

        }

        /// <summary>
        /// One less than the number of entries following the header
        /// u32 count (0x00)
        /// </summary>
        internal uint Count
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, value); }
        }

        /// <summary>
        /// The starting byte offset of the block in the Inode Table where the inodes are stored
        /// u32 start (0x04)
        /// </summary>
        internal uint Start
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// An arbitrary inode number. The entries that follow store their inode number as a difference to this. 
        /// Typically the inode numbers are allocated in a continuous sequence for all children of a directory and the header simply stores the first one. 
        /// Hard links of course break the sequence and require a new header if they are further away than +/- 32k of this number. 
        /// Inode number allocation and picking of the reference could of course be optimized to prevent this.
        /// u32 inode number (0x08)
        /// </summary>
        internal uint INodeNumber
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

    }
}
