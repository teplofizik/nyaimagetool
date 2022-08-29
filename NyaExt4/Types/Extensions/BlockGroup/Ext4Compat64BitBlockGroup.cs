using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types.Extensions.BlockGroup
{
    class Ext4Compat64BitBlockGroup : ExtBlockGroup
    {
        public Ext4Compat64BitBlockGroup(byte[] Data, long Offset) : base(Data, Offset)
        {

        }

        // These fields only exist if the 64bit feature is enabled and s_desc_size > 32.

        /// <summary>
        /// Upper 32-bits of location of block bitmap.
        /// u32 bg_block_bitmap_hi (0x20)
        /// </summary>
        public uint BlockBitmapHi
        {
            get { return ReadUInt32(0x20); }
            set { WriteUInt32(0x20, value); }
        }

        /// <summary>
        /// Upper 32-bits of location of block bitmap.
        /// u32 bg_inode_bitmap_hi (0x24)
        /// </summary>
        public uint INodeBitmapHi
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }

        /// <summary>
        /// Upper 32-bits of location of inode table.
        /// u32 bg_inode_table_hi (0x28)
        /// </summary>
        public uint INodeTableHi
        {
            get { return ReadUInt32(0x28); }
            set { WriteUInt32(0x28, value); }
        }

        /// <summary>
        /// Upper 16-bits of free block count.
        /// u16 bg_free_blocks_count_hi (0x2C)
        /// </summary>
        public uint FreeBlocksCountHi
        {
            get { return ReadUInt16(0x2C); }
            set { WriteUInt16(0x2C, value); }
        }

        /// <summary>
        /// Upper 16-bits of free inode count.
        /// u16 bg_free_inodes_count_hi (0x2E)
        /// </summary>
        public uint FreeINodesCountHi
        {
            get { return ReadUInt16(0x2E); }
            set { WriteUInt16(0x2E, value); }
        }

        /// <summary>
        /// Upper 16-bits of directory count.
        /// u16 bg_used_dirs_count_hi (0x30)
        /// </summary>
        public uint UsedDirsCountHi
        {
            get { return ReadUInt16(0x30); }
            set { WriteUInt16(0x30, value); }
        }

        /// <summary>
        /// Upper 16-bits of unused inode count. 
        /// u16 bg_itable_unused_hi (0x32)
        /// </summary>
        public uint ITableUnusedHi
        {
            get { return ReadUInt16(0x32); }
            set { WriteUInt16(0x32, value); }
        }

        /// <summary>
        /// Upper 32-bits of location of snapshot exclusion bitmap.
        /// u32 bg_exclude_bitmap_hi (0x34)
        /// </summary>
        public uint ExcludeBitmapHi
        {
            get { return ReadUInt32(0x34); }
            set { WriteUInt32(0x34, value); }
        }

        /// <summary>
        /// Upper 16-bits of the block bitmap checksum.
        /// u16 bg_block_bitmap_csum_hi (0x38)
        /// </summary>
        public uint BlockBitmapChecksumHi
        {
            get { return ReadUInt16(0x38); }
            set { WriteUInt16(0x38, value); }
        }

        /// <summary>
        /// Upper 16-bits of the inode bitmap checksum.
        /// u16 bg_inode_bitmap_csum_hi (0x3A)
        /// </summary>
        public uint INodeBitmapChecksumHi
        {
            get { return ReadUInt16(0x3A); }
            set { WriteUInt16(0x3A, value); }
        }
    }
}
