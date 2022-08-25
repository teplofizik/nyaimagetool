using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt4.Types
{
    class ExtBlockGroup : RawPacket
    {
        public ExtBlockGroup(byte[] Data) : base(Data)
        {

        }

        /// <summary>
        /// Lower 32-bits of location of block bitmap.
        /// u32 bg_block_bitmap_lo (0x00)
        /// </summary>
        public uint BlockBitmapLo
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, value); }
        }

        /// <summary>
        /// Lower 32-bits of location of block bitmap.
        /// u32 bg_inode_bitmap_lo (0x04)
        /// </summary>
        public uint INodeBitmapLo
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// Lower 32-bits of location of inode table.
        /// u32 bg_inode_table_lo (0x08)
        /// </summary>
        public uint INodeTableLo
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// Lower 16-bits of free block count.
        /// u16 bg_free_blocks_count_lo (0x0C)
        /// </summary>
        public uint FreeBlocksCountLo
        {
            get { return ReadUInt16(0x0C); }
            set { WriteUInt16(0x0C, value); }
        }

        /// <summary>
        /// Lower 16-bits of free inode count.
        /// u16 bg_free_inodes_count_lo (0x0E)
        /// </summary>
        public uint FreeINodesCountLo
        {
            get { return ReadUInt16(0x0E); }
            set { WriteUInt16(0x0E, value); }
        }

        /// <summary>
        /// Lower 16-bits of directory count.
        /// u16 bg_used_dirs_count_lo (0x10)
        /// </summary>
        public uint UsedDirsCountLo
        {
            get { return ReadUInt16(0x10); }
            set { WriteUInt16(0x10, value); }
        }

        /// <summary>
        /// Block group flags. Any of:
        ///  0x1	inode table and bitmap are not initialized (EXT4_BG_INODE_UNINIT).
        ///  0x2	block bitmap is not initialized (EXT4_BG_BLOCK_UNINIT).
        ///  0x4	inode table is zeroed (EXT4_BG_INODE_ZEROED).
        /// u16 bg_flags (0x12)
        /// </summary>
        public uint Flags
        {
            get { return ReadUInt16(0x12); }
            set { WriteUInt16(0x12, value); }
        }

        /// <summary>
        /// Lower 32-bits of location of snapshot exclusion bitmap.
        /// u32 bg_exclude_bitmap_lo (0x14)
        /// </summary>
        public uint ExcludeBitmapLo
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Lower 16-bits of the block bitmap checksum.
        /// u16 bg_block_bitmap_csum_lo (0x18)
        /// </summary>
        public uint BlockBitmapChecksumLo
        {
            get { return ReadUInt16(0x18); }
            set { WriteUInt16(0x18, value); }
        }

        /// <summary>
        /// Lower 16-bits of the inode bitmap checksum.
        /// u16 bg_inode_bitmap_csum_lo (0x1A)
        /// </summary>
        public uint INodeBitmapChecksumLo
        {
            get { return ReadUInt16(0x1A); }
            set { WriteUInt16(0x1A, value); }
        }

        /// <summary>
        /// Lower 16-bits of unused inode count. 
        /// If set, we needn't scan past the (sb.s_inodes_per_group - gdt.bg_itable_unused)th entry in the inode table for this group.
        /// u16 bg_itable_unused_lo (0x1C)
        /// </summary>
        public uint ITableUnusedLo
        {
            get { return ReadUInt16(0x1C); }
            set { WriteUInt16(0x1C, value); }
        }

        /// <summary>
        /// Group descriptor checksum; 
        /// crc16(sb_uuid+group+desc) if the RO_COMPAT_GDT_CSUM feature is set, or crc32c(sb_uuid+group_desc) & 0xFFFF if the RO_COMPAT_METADATA_CSUM feature is set.
        /// u16 bg_checksum (0x1E)
        /// </summary>
        public uint Checksum
        {
            get { return ReadUInt16(0x1E); }
            set { WriteUInt16(0x1E, value); }
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
