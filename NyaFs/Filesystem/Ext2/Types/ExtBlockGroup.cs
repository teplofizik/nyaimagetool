using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Ext2.Types
{
    public class ExtBlockGroup : ArrayWrapper
    {
        public readonly uint Id;

        public ExtBlockGroup(byte[] Data, uint Id, long Offset) : base(Data, Offset, 0x20)
        {
            this.Id = Id;
        }

        public uint GetLocalNodeIndex(uint NodeId, uint NodesPerGroup) => NodeId - NodesPerGroup * Id;

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
    }
}