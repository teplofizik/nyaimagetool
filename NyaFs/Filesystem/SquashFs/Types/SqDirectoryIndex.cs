using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    class SqDirectoryIndex : ArrayWrapper
    {
        public SqDirectoryIndex(byte[] Data, long Offset) : base(Data, Offset, 0x0C + 1 + Data.ReadUInt32(Offset + 8))
        {

        }

        /// <summary>
        /// This stores a byte offset from the first directory header to the current header, as if the uncompressed directory metadata blocks were laid out in memory consecutively.
        /// u32 index (0x00)
        /// </summary>
        internal uint Index
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, value); }
        }

        /// <summary>
        /// Start offset of a directory table metadata block
        /// u32 start (0x04)
        /// </summary>
        internal uint Start
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// One less than the size of the entry name
        /// u32 name_size (0x08)
        /// </summary>
        internal uint NameSize
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// Name of first entry
        /// char[name_size+1] name (0x0C)
        /// </summary>
        internal string Name => ReadString(0x0C, NameSize + 1);

        /// <summary>
        /// Total size of index entry
        /// </summary>
        public long TotalSize => 0x0C + NameSize + 1;
    }
}
