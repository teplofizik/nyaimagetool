using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types
{
    class ExtDirectoryEntry : ArrayWrapper
    {
        /// <summary>
        /// Wrapper for directory struct
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        public ExtDirectoryEntry(byte[] Data, long Offset) : base(Data, Offset, 255) // ext2, ext3 => 128 bytes
        {

        }

        /// <summary>
        /// Number of the inode that this directory entry points to.
        /// </summary>
        public uint INode
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// Length of this directory entry. Must be a multiple of 4.
        /// </summary>
        public uint RecordLength
        {
            get { return ReadUInt16(4); }
            set { WriteUInt16(4, value); }
        }

        /// <summary>
        /// Length of the file name.
        /// </summary>
        public uint NameLength
        {
            get { return ReadUInt16(6); }
            set { WriteUInt16(6, value); }
        }

        public string Name => ReadString(8, NameLength);

    }
}
