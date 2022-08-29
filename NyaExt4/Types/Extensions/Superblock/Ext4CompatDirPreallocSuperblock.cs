using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types.Extensions.Superblock
{
    class Ext4CompatDirPreallocSuperblock : ArrayWrapper
    {
        public Ext4CompatDirPreallocSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x400)
        {

        }

        //
        // Performance hints. Directory preallocation should only happen if the EXT4_FEATURE_COMPAT_DIR_PREALLOC flag is on.
        //

        /// <summary>
        /// # of blocks to try to preallocate for ... files? (Not used in e2fsprogs/Linux)
        /// u8 s_prealloc_blocks (0xCC)
        /// </summary>
        public byte PreallocBlocks
        {
            get { return ReadByte(0xCC); }
            set { WriteByte(0xCC, value); }
        }

        /// <summary>
        /// # of blocks to preallocate for directories. (Not used in e2fsprogs/Linux)
        /// u8 s_prealloc_dir_blocks (0xCD)
        /// </summary>
        public byte PreallocDirBlocks
        {
            get { return ReadByte(0xCD); }
            set { WriteByte(0xCD, value); }
        }

        /// <summary>
        /// Number of reserved GDT entries for future filesystem expansion.
        /// u16 s_reserved_gdt_blocks (0xCE)
        /// </summary>
        public uint ReservedGDTBlocks
        {
            get { return ReadUInt16(0xCE); }
            set { WriteUInt16(0xCE, value); }
        }

    }
}
