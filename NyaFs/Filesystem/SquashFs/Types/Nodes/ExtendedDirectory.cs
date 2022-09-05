using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types.Nodes
{
    class ExtendedDirectory : SqInode
    {
        private long IndexesSize = 0;

        public ExtendedDirectory(byte[] Data) : base(Data, 0x28)
        {
            /*var Idx = new List<SqDirectoryIndex>();
            long Offset = 0x28;
            for(int i = 0; i < IndexCount; i++)
            {
                var DI = new SqDirectoryIndex(Data, 0x28 + Offset);
                IndexesSize += DI.TotalSize;
                Offset += DI.TotalSize;
            }
            Indexes = Idx.ToArray();*/
        }

        /// <summary>
        /// INode size
        /// </summary>
        internal override long INodeSize => 0x28 + IndexesSize;

        /// <summary>
        /// The number of hard links to this directory. 
        /// Note that for historical reasons, the hard link count of a directory includes the number of entries in the directory and is initialized to 2 for an empty directory. 
        /// I.e. a directory with N entries has at least N + 2 link count.
        /// u32 hard_link_count (0x10)
        /// </summary>
        public uint HardLinkCount
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// Total (uncompressed) size in bytes of the entries in the Directory Table, including headers plus 3. 
        /// The extra 3 bytes are for a virtual "." and ".." item in each directory which is not written, but can be considered to be part of the logical size of the directory.
        /// u16 file_size (0x14)
        /// </summary>
        public uint FileSize
        {
            get { return ReadUInt16(0x14); }
            set { WriteUInt16(0x14, value); }
        }

        /// <summary>
        /// The location of the of the block in the Directory Table where the directory entry information starts
        /// u32 dir_block_start (0x10)
        /// </summary>
        public uint DirBlockStart
        {
            get { return ReadUInt32(0x18); }
            set { WriteUInt32(0x18, value); }
        }

        /// <summary>
        /// The inode_number of the parent of this directory. If this is the root directory, this will be 1
        /// u32 parent_inode_number (0x1C)
        /// </summary>
        public uint ParentINodeNumber
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// The number of directory index entries following the inode structure
        /// u16 index_count (0x20)
        /// </summary>
        public uint IndexCount
        {
            get { return ReadUInt16(0x20); }
            set { WriteUInt16(0x20, value); }
        }

        /// <summary>
        /// The (uncompressed) offset within the block in the Directory Table where the directory entry information starts
        /// u16 block_offset (0x22)
        /// </summary>
        public uint BlockOffset
        {
            get { return ReadUInt16(0x22); }
            set { WriteUInt16(0x22, value); }
        }

        /// <summary>
        /// An index into the xattr lookup table. Set to 0xFFFFFFFF if the inode has no extended attributes
        /// u32 xattr_idx (0x24)
        /// </summary>
        public uint XAttrIndex
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }

        public SqMetadataRef NodeReference => new SqMetadataRef(DirBlockStart, BlockOffset);

        //public readonly SqDirectoryIndex[] Indexes;
    }
}
