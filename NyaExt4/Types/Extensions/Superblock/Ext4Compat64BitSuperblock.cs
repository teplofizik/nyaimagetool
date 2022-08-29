using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types.Extensions.Superblock
{
    class Ext4Compat64BitSuperblock : ArrayWrapper
    {
        public Ext4Compat64BitSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x400)
        {

        }

        //
        // 64bit support valid if EXT4_FEATURE_COMPAT_64BIT
        //

        /// <summary>
        /// High 32-bits of the block count.
        /// u32 s_blocks_count_hi (0x150) 
        /// </summary>
        public uint BlocksCountHi
        {
            get { return ReadUInt32(0x150); }
            set { WriteUInt32(0x150, value); }
        }

        /// <summary>
        /// High 32-bits of the reserved block count.
        /// u32 s_r_blocks_count_hi (0x154) 
        /// </summary>
        public uint ReservedBlocksCountHi
        {
            get { return ReadUInt32(0x154); }
            set { WriteUInt32(0x154, value); }
        }

        /// <summary>
        /// High 32-bits of the free block count.
        /// u32 s_free_blocks_count_hi (0x158) 
        /// </summary>
        public uint FreeBlocksCountHi
        {
            get { return ReadUInt32(0x158); }
            set { WriteUInt32(0x158, value); }
        }

        /// <summary>
        /// All inodes have at least # bytes.
        /// u16 s_min_extra_isize (0x15C)
        /// </summary>
        public uint MinExtraISize
        {
            get { return ReadUInt16(0x15C); }
            set { WriteUInt16(0x15C, value); }
        }

        /// <summary>
        /// New inodes should reserve # bytes.
        /// u16 s_want_extra_isize (0x15E)
        /// </summary>
        public uint WantExtraISize
        {
            get { return ReadUInt16(0x15E); }
            set { WriteUInt16(0x15E, value); }
        }

        /// <summary>
        /// Miscellaneous flags. Any of:
        ///  0x0001	Signed directory hash in use.
        ///  0x0002	Unsigned directory hash in use.
        ///  0x0004	To test development code.
        /// u32 s_flags (0x160)
        /// </summary>
        public uint Flags
        {
            get { return ReadUInt32(0x160); }
            set { WriteUInt32(0x160, value); }
        }

        /// <summary>
        /// RAID stride. This is the number of logical blocks read from or written to the disk before moving to the next disk. 
        /// This affects the placement of filesystem metadata, which will hopefully make RAID storage faster.
        /// u16 s_raid_stride (0x164)
        /// </summary>
        public uint RAIDStride
        {
            get { return ReadUInt16(0x164); }
            set { WriteUInt16(0x164, value); }
        }

        /// <summary>
        /// # seconds to wait in multi-mount prevention (MMP) checking. In theory, MMP is a mechanism to record in the superblock which host 
        /// and device have mounted the filesystem, in order to prevent multiple mounts. This feature does not seem to be implemented...
        /// u16 s_mmp_interval (0x166)
        /// </summary>
        public uint MMPInterval
        {
            get { return ReadUInt16(0x166); }
            set { WriteUInt16(0x166, value); }
        }

        /// <summary>
        /// Block # for multi-mount protection data.
        /// u64 s_mmp_block (0x168)
        /// </summary>
        public ulong MMPBlock
        {
            get { return ReadUInt64(0x168); }
            set { WriteUInt64(0x168, value); }
        }

        /// <summary>
        /// RAID stripe width. This is the number of logical blocks read from or written to the disk before coming back to the current disk.
        /// This is used by the block allocator to try to reduce the number of read-modify-write operations in a RAID5/6.
        /// u32 s_raid_stripe_width (0x170)
        /// </summary>
        public uint RAIDStripeWidth
        {
            get { return ReadUInt32(0x170); }
            set { WriteUInt32(0x170, value); }
        }

        /// <summary>
        /// Size of a flexible block group is 2 ^ s_log_groups_per_flex
        /// u8 s_log_groups_per_flex (0x174)
        /// </summary>
        public byte LogGroupsPerFlex
        {
            get { return ReadByte(0x174); }
            set { WriteByte(0x174, value); }
        }

        /// <summary>
        /// Metadata checksum algorithm type. The only valid value is 1 (crc32c).
        /// u8 s_checksum_type (0x175)
        /// </summary>
        public byte ChecksumType
        {
            get { return ReadByte(0x175); }
            set { WriteByte(0x175, value); }
        }

        /// <summary>
        /// Number of KiB written to this filesystem over its lifetime.
        /// u64 s_kbytes_written (0x178)
        /// </summary>
        public ulong KBytesWritten
        {
            get { return ReadUInt64(0x178); }
            set { WriteUInt64(0x178, value); }
        }

        /// <summary>
        /// inode number of active snapshot. (Not used in e2fsprogs/Linux.)
        /// u32 s_snapshot_inum (0x180)
        /// </summary>
        public uint SnapshotINum
        {
            get { return ReadUInt32(0x180); }
            set { WriteUInt32(0x180, value); }
        }

        /// <summary>
        /// Sequential ID of active snapshot. (Not used in e2fsprogs/Linux.)
        /// u32 s_snapshot_id (0x184)
        /// </summary>
        public uint SnapshotId
        {
            get { return ReadUInt32(0x184); }
            set { WriteUInt32(0x184, value); }
        }

        /// <summary>
        /// Number of blocks reserved for active snapshot's future use. (Not used in e2fsprogs/Linux.)
        /// u64 s_snapshot_list (0x190)
        /// </summary>
        public ulong SnapshotList
        {
            get { return ReadUInt64(0x190); }
            set { WriteUInt64(0x190, value); }
        }

        /// <summary>
        /// Number of errors seen.
        /// u32 s_error_count (0x194)
        /// </summary>
        public uint ErrorCount
        {
            get { return ReadUInt32(0x194); }
            set { WriteUInt32(0x194, value); }
        }

        /// <summary>
        /// First time an error happened, in seconds since the epoch.
        /// u32 s_first_error_time (0x198)
        /// </summary>
        public uint FirstErrorTime
        {
            get { return ReadUInt32(0x198); }
            set { WriteUInt32(0x198, value); }
        }

        /// <summary>
        /// inode involved in first error.
        /// u32 s_first_error_ino (0x19C)
        /// </summary>
        public uint FirstErrorInode
        {
            get { return ReadUInt32(0x19C); }
            set { WriteUInt32(0x19C, value); }
        }

        /// <summary>
        /// Number of block involved of first error.
        /// u64 s_first_error_block (0x1A0)
        /// </summary>
        public ulong FirstErrorBlock
        {
            get { return ReadUInt64(0x1A0); }
            set { WriteUInt64(0x1A0, value); }
        }

        /// <summary>
        /// Name of function where the error happened.
        /// char s_first_error_func[32] (0x1A8)
        /// </summary>
        public string FirstErrorFunc
        {
            get { return ReadString(0x1A8, 32); }
            set { WriteString(0x1A8, value, 32); }
        }

        /// <summary>
        /// Line number where error happened.
        /// u32 s_first_error_line (0x1C8)
        /// </summary>
        public uint FirstErrorLine
        {
            get { return ReadUInt32(0x1C8); }
            set { WriteUInt32(0x1C8, value); }
        }

        /// <summary>
        /// Time of most recent error, in seconds since the epoch.
        /// u32 s_last_error_time (0x1CC)
        /// </summary>
        public uint LastErrorTime
        {
            get { return ReadUInt32(0x1CC); }
            set { WriteUInt32(0x1CC, value); }
        }

        /// <summary>
        /// inode involved in most recent error.
        /// u32 s_last_error_ino (0x1D0)
        /// </summary>
        public uint LastErrorINode
        {
            get { return ReadUInt32(0x1D0); }
            set { WriteUInt32(0x1D0, value); }
        }

        /// <summary>
        /// Line number where most recent error happened.
        /// u32 s_last_error_line (0x1D4)
        /// </summary>
        public uint LastErrorLine
        {
            get { return ReadUInt32(0x1D4); }
            set { WriteUInt32(0x1D4, value); }
        }

        /// <summary>
        /// Number of block involved in most recent error.
        /// u64 s_last_error_block (0x1D8)
        /// </summary>
        public ulong LastErrorBlock
        {
            get { return ReadUInt64(0x1D8); }
            set { WriteUInt64(0x1D8, value); }
        }

        /// <summary>
        /// Name of function where the most recent error happened.
        /// char s_last_error_func[32] (0x1E0)
        /// </summary>
        public string LastErrorFunc
        {
            get { return ReadString(0x1E0, 32); }
            set { WriteString(0x1E0, value, 32); }
        }

        /// <summary>
        /// ASCIIZ string of mount options
        /// char s_mount_opts[64] (0x200)
        /// </summary>
        public string MountOpts
        {
            get { return ReadString(0x200, 64); }
            set { WriteString(0x200, value, 64); }
        }

        /// <summary>
        /// Inode number of user quota file.
        /// u32 s_usr_quota_inum (0x240)
        /// </summary>
        public uint UsrQuotaINum
        {
            get { return ReadUInt32(0x240); }
            set { WriteUInt32(0x240, value); }
        }

        /// <summary>
        /// Inode number of group quota file.
        /// u32 s_grp_quota_inum (0x244)
        /// </summary>
        public uint GrpQuotaINum
        {
            get { return ReadUInt32(0x244); }
            set { WriteUInt32(0x240, value); }
        }

        /// <summary>
        /// Overhead blocks/clusters in fs. (Huh? This field is always zero, which means that the kernel calculates it dynamically.)
        /// u32 s_overhead_blocks (0x248)
        /// </summary>
        public uint OverheadBlocks
        {
            get { return ReadUInt32(0x248); }
            set { WriteUInt32(0x248, value); }
        }

        /// <summary>
        /// Block groups containing superblock backups (if sparse_super2)
        /// u32 s_backup_bgs[2] (0x24C)
        /// </summary>
        public uint[] BackupsBlockGroups
        {
            get { return ReadUInt32Array(0x24C, 2); }
            set
            {
                if (value == null) throw new ArgumentNullException("Backups block groups must be not-null value");
                if (value.Length != 2) throw new ArgumentException("Backups block groups must have 2 elements");

                WriteUInt32(0x24C, value[0]);
                WriteUInt32(0x250, value[1]);
            }
        }

        /// <summary>
        /// Encryption algorithms in use. There can be up to four algorithms in use at any time; valid algorithm codes are given below:
        ///  0	Invalid algorithm (ENCRYPTION_MODE_INVALID).
        ///  1	256-bit AES in XTS mode (ENCRYPTION_MODE_AES_256_XTS).
        ///  2	256-bit AES in GCM mode (ENCRYPTION_MODE_AES_256_GCM).
        ///  3	256-bit AES in CBC mode (ENCRYPTION_MODE_AES_256_CBC).
        /// u8 s_encrypt_algos[4] (0x254)
        /// </summary>
        public byte[] EncryptAlgorithms
        {
            get { return ReadArray(0x254, 4); }
            set
            {
                if (value == null) throw new ArgumentNullException("Encrypt algorithms must be not-null value");
                if (value.Length != 4) throw new ArgumentException("Encrypt algorithms must have 4 elements");

                WriteArray(0x254, value, 4);
            }
        }

        /// <summary>
        /// Salt for the string2key algorithm for encryption.
        /// u8 s_encrypt_pw_salt[16] (0x258)
        /// </summary>
        public byte[] EncryptPWSalt
        {
            get { return ReadArray(0x258, 16); }
            set
            {
                if (value == null) throw new ArgumentNullException("Encrypt pw salt must be not-null value");
                if (value.Length != 16) throw new ArgumentException("Encrypt pw salt must have 16 elements");

                WriteArray(0x258, value, 16);
            }
        }

        /// <summary>
        /// Inode number of lost+found
        /// u32 s_lpf_ino (0x268)
        /// </summary>
        public uint LpFINode
        {
            get { return ReadUInt32(0x268); }
            set { WriteUInt32(0x268, value); }
        }

        /// <summary>
        /// Inode that tracks project quotas.
        /// u32 s_prj_quota_inum (0x26C)
        /// </summary>
        public uint ProjectQuotaINum
        {
            get { return ReadUInt32(0x26C); }
            set { WriteUInt32(0x26C, value); }
        }

        /// <summary>
        /// Checksum seed used for metadata_csum calculations. This value is crc32c(~0, $orig_fs_uuid).
        /// u32 s_checksum_seed (0x270)
        /// </summary>
        public uint ChecksumSeed
        {
            get { return ReadUInt32(0x270); }
            set { WriteUInt32(0x270, value); }
        }

        /// <summary>
        /// Superblock checksum.
        /// u32 s_checksum (0x3FC)
        /// </summary>
        public uint Checksum
        {
            get { return ReadUInt32(0x3FC); }
            set { WriteUInt32(0x3FC, value); }
        }
    }
}
