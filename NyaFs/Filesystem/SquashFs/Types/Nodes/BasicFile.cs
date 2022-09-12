using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types.Nodes
{
    class BasicFile : SqInode
    {
        uint BlockSize;

        private static int CalcNodeLength(uint[] BlocksSizes) => 0x20 + BlocksSizes.Length * 4;

        public BasicFile(uint Mode, uint User, uint Group, uint BlocksStart, uint FileSize, uint FragmentBlockIndex, uint FragmentBlockOffset, uint[] BlocksSizes) : base(CalcNodeLength(BlocksSizes))
        {
            InodeType = SqInodeType.BasicFile;
            Permissions = Mode;
            GidIndex = Group;
            UidIndex = User;
            this.BlocksStart = BlocksStart;

            this.FragmentBlockIndex = FragmentBlockIndex;
            this.FragmentBlockOffset = FragmentBlockOffset;
            this.FileSize = FileSize;
            for(int i = 0; i < BlocksSizes.Length; i++)
                WriteUInt32(0x20 + i * 4, BlocksSizes[i]);
        }


        public BasicFile(byte[] Data, uint BlockSize) : base(Data, 0x20)
        {
            this.BlockSize = BlockSize;
        }

        /// <summary>
        /// INode size
        /// </summary>
        internal override long INodeSize => 0x20 + BlockCount * 4; // TODO: add blocks_sizes calculation...

        /// <summary>
        /// The offset from the start of the archive where the data blocks are stored
        /// u32 blocks_start (0x10)
        /// </summary>
        public uint BlocksStart
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// The index of a fragment entry in the fragment table which describes the data block the fragment of this file is stored in. 
        /// If this file does not end with a fragment, this should be 0xFFFFFFFF
        /// u32 fragment_block_index (0x14)
        /// </summary>
        public uint FragmentBlockIndex
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// The (uncompressed) offset within the fragment data block where the fragment for this file. 
        /// Information about the fragment can be found at fragment_block_index. 
        /// The size of the fragment can be found as file_size % superblock.block_size. 
        /// If this file does not end with a fragment, the value of this field is undefined (probably zero)
        /// u32 block_offset (0x18)
        /// </summary>
        public uint FragmentBlockOffset
        {
            get { return ReadUInt32(0x18); }
            set { WriteUInt32(0x18, value); }
        }

        /// <summary>
        /// The (uncompressed) size of this file
        /// u32 file_size (0x1C)
        /// </summary>
        public uint FileSize
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// The size of the fragment with id=FragmentBlockIndex
        /// </summary>
        public long FragmentSize => FileSize % BlockSize;

        /// <summary>
        /// Count of block sizes table
        /// </summary>
        public long BlockCount => (FragmentBlockIndex == 0xFFFFFFFF) ? (FileSize + BlockSize - 1) / BlockSize : FileSize / BlockSize;

        /// <summary>
        /// A list of block sizes. If this file ends in a fragment, the size of this list is the number of full data blocks needed to store 
        /// file_size bytes. If this file does not have a fragment, the size of the list is the number of blocks needed to store file_size bytes, 
        /// rounded up. Each item in the list describes the (possibly compressed) size of a block. 
        /// See datablocks & fragments for information about how to interpret this size.
        /// u32 block_sizes[BlockCount]
        /// </summary>
        public uint[] BlockSizes => ReadUInt32Array(0x20, BlockCount);
    }
}
