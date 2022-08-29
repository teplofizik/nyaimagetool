using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types.Extensions.Superblock
{
    class Ext4CompatHasJournalSuperblock : ArrayWrapper
    {
        public Ext4CompatHasJournalSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x400)
        {

        }

        //
        // Journaling support valid if EXT4_FEATURE_COMPAT_HAS_JOURNAL set.
        //

        /// <summary>
        /// UUID of journal superblock.
        /// u8 s_journal_uuid[16] (0xD0)
        /// </summary>
        public byte[] JournalUUID
        {
            get { return ReadArray(0xD0, 0x10); }
            set
            {
                if (value == null) throw new ArgumentNullException("UUID must be not null value");
                if (value.Length != 0x10) throw new ArgumentException("UUID length must be 16 bytes");

                WriteArray(0xD0, value, 0x10);
            }
        }

        /// <summary>
        /// inode number of journal file.
        /// u32 s_journal_inum (0xE0)
        /// </summary>
        public uint JournalINodeNumber
        {
            get { return ReadUInt32(0xE0); }
            set { WriteUInt32(0xE0, value); }
        }

        /// <summary>
        /// Device number of journal file, if the external journal feature flag is set.
        /// u32 s_journal_dev (0xE4)
        /// </summary>
        public uint JournalDevice
        {
            get { return ReadUInt32(0xE4); }
            set { WriteUInt32(0xE4, value); }
        }

        /// <summary>
        /// Start of list of orphaned inodes to delete.
        /// u32 s_last_orphan (0xE8)
        /// </summary>
        public uint LastOrphan
        {
            get { return ReadUInt32(0xE8); }
            set { WriteUInt32(0xE8, value); }
        }

        /// <summary>
        /// HTREE hash seed.
        /// u32 s_hash_seed[4] (0xEC)
        /// </summary>
        public uint[] HashSeed
        {
            get { return ReadUInt32Array(0xEC, 4); }
            set
            {
                if (value == null) throw new ArgumentNullException("Hash seed must be not-null value");
                if (value.Length != 4) throw new ArgumentException("Hash seed must have 4 elements");

                WriteUInt32(0xEC, HashSeed[0]);
                WriteUInt32(0xF0, HashSeed[1]);
                WriteUInt32(0xF4, HashSeed[2]);
                WriteUInt32(0xF8, HashSeed[3]);
            }
        }

        /// <summary>
        /// Default hash algorithm to use for directory hashes. One of:
        /// 0x0	Legacy.
        /// 0x1	Half MD4.
        /// 0x2	Tea.
        /// 0x3	Legacy, unsigned.
        /// 0x4	Half MD4, unsigned.
        /// 0x5	Tea, unsigned.
        /// u8 s_def_hash_version (0xFC)
        /// </summary>
        public byte DefaultHashVersion
        {
            get { return ReadByte(0xFC); }
            set { WriteByte(0xFC, value); }
        }

        /// <summary>
        /// If this value is 0 or EXT3_JNL_BACKUP_BLOCKS (1), then the s_jnl_blocks field contains a duplicate copy of the inode's i_block[] array and i_size.
        /// u8 s_jnl_backup_type (0xFD)
        /// </summary>
        public byte JournalBackupType
        {
            get { return ReadByte(0xFD); }
            set { WriteByte(0xFD, value); }
        }

        /// <summary>
        /// Size of group descriptors, in bytes, if the 64bit incompat feature flag is set.
        /// u16 s_desc_size (0xFE)
        /// </summary>
        public uint DescriptorSize
        {
            get { return ReadUInt16(0xFE); }
            set { WriteUInt16(0xFE, value); }
        }

        /// <summary>
        /// Default mount options. Any of:
        ///  0x0001 Print debugging info upon (re)mount. (EXT4_DEFM_DEBUG)
        ///  0x0002 New files take the gid of the containing directory (instead of the fsgid of the current process). (EXT4_DEFM_BSDGROUPS)
        ///  0x0004 Support userspace-provided extended attributes. (EXT4_DEFM_XATTR_USER)
        ///  0x0008 Support POSIX access control lists (ACLs). (EXT4_DEFM_ACL)
        ///  0x0010 Do not support 32-bit UIDs. (EXT4_DEFM_UID16)
        ///  0x0020 All data and metadata are commited to the journal. (EXT4_DEFM_JMODE_DATA)
        ///  0x0040 All data are flushed to the disk before metadata are committed to the journal. (EXT4_DEFM_JMODE_ORDERED)
        ///  0x0060 Data ordering is not preserved; data may be written after the metadata has been written. (EXT4_DEFM_JMODE_WBACK)
        ///  0x0100 Disable write flushes. (EXT4_DEFM_NOBARRIER)
        ///  0x0200	Track which blocks in a filesystem are metadata and therefore should not be used as data blocks. 
        ///         This option will be enabled by default on 3.18, hopefully. (EXT4_DEFM_BLOCK_VALIDITY)
        ///  0x0400	Enable DISCARD support, where the storage device is told about blocks becoming unused. (EXT4_DEFM_DISCARD)
        ///  0x0800	Disable delayed allocation. (EXT4_DEFM_NODELALLOC)
        /// u32 s_default_mount_opts (0x100)
        /// </summary>
        public uint DefaultMountOptions
        {
            get { return ReadUInt32(0x100); }
            set { WriteUInt32(0x100, value); }
        }

        /// <summary>
        /// First metablock block group, if the meta_bg feature is enabled.
        /// u32 s_first_meta_bg (0x104)
        /// </summary>
        public uint FirstMetablockBlockGroup
        {
            get { return ReadUInt32(0x104); }
            set { WriteUInt32(0x104, value); }
        }

        /// <summary>
        /// When the filesystem was created, in seconds since the epoch.
        /// u32 s_mkfs_time (0x108)
        /// </summary>
        public uint MkfsTime
        {
            get { return ReadUInt32(0x108); }
            set { WriteUInt32(0x108, value); }
        }

        /// <summary>
        /// Backup copy of the journal inode's i_block[] array in the first 15 elements and i_size_high and i_size in the 16th and 17th elements, respectively.
        /// u32 s_jnl_blocks[17] (0x10C)
        /// </summary>
        public uint[] JournalBlocks
        {
            get { return ReadUInt32Array(0x10C, 17); }
            set
            {
                if (value == null) throw new ArgumentNullException("Journal blocks must be not-null value");
                if (value.Length != 17) throw new ArgumentException("Journal blocks must have 17 elements");

                for (int i = 0; i < 17; i++)
                    WriteUInt32(0x10C + 4 * i, value[i]);
            }
        }
    }
}
