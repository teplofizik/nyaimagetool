using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt4.Types
{
    /// <summary>
    /// The Super Block of Ext Filesystem
    /// https://ext4.wiki.kernel.org/index.php/Ext4_Disk_Layout
    /// </summary>
    public class ExtSuperBlock : RawPacket
    {
        public ExtSuperBlock(byte[] Data) : base(Data)
        {

        }

        /// <summary>
        /// Total inode count.
        /// u32 s_inodes_count (0x00)
        /// </summary>
        public uint INodesCount
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, value); }
        }

        /// <summary>
        /// Total block count.
        /// u32 s_blocks_count_lo (0x04)
        /// </summary>
        public uint BlocksCount
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// This number of blocks can only be allocated by the super-user.
        /// u32 s_r_blocks_count_lo (0x08)
        /// </summary>
        public uint RootBlocksCount
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }


        /// <summary>
        /// Free block count.
        /// u32 s_free_blocks_count_lo (0x0C)
        /// </summary>
        public uint FreeBlocksCount
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }

        /// <summary>
        /// Free inode count.
        /// u32 s_free_inodes_count (0x10)
        /// </summary>
        public uint FreeINodeCount
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// First data block. This must be at least 1 for 1k-block filesystems and is typically 0 for all other block sizes.
        /// u32 s_first_data_block (0x14)
        /// </summary>
        public uint FirstDataBlock
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Block size is 2 ^ (10 + s_log_block_size).
        /// u32 s_log_block_size (0x18)
        /// </summary>
        public uint LogBlockSize
        {
            get { return ReadUInt32(0x18); }
            set { WriteUInt32(0x18, value); }
        }

        /// <summary>
        /// Block size (read only)
        /// </summary>
        public uint BlockSize => Convert.ToUInt32(Math.Pow(2, 10 + LogBlockSize));

        /// <summary>
        /// Cluster size is (2 ^ s_log_cluster_size) blocks if bigalloc is enabled. Otherwise s_log_cluster_size must equal s_log_block_size.
        /// u32 s_log_cluster_size (0x1C)
        /// </summary>
        public uint LogClusterSize
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// Block size (read only)
        /// </summary>
        public uint ClusterSize => Convert.ToUInt32(Math.Pow(2, LogClusterSize));

        /// <summary>
        /// Blocks per group.
        /// u32 s_blocks_per_group (0x20)
        /// </summary>
        public uint BlocksPerGroup
        {
            get { return ReadUInt32(0x20); }
            set { WriteUInt32(0x20, value); }
        }

        /// <summary>
        /// Clusters per group, if bigalloc is enabled. Otherwise s_clusters_per_group must equal s_blocks_per_group.
        /// u32 s_clusters_per_group (0x24)
        /// </summary>
        public uint ClustersPerGroup
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }

        /// <summary>
        /// Inodes per group.
        /// u32 s_inodes_per_group (0x28)
        /// </summary>
        public uint InodesPerGroup
        {
            get { return ReadUInt32(0x28); }
            set { WriteUInt32(0x28, value); }
        }

        /// <summary>
        /// Mount time, in seconds since the epoch.
        /// u32 s_mtime (0x2C)
        /// </summary>
        public uint MTime
        {
            get { return ReadUInt32(0x2C); }
            set { WriteUInt32(0x2C, value); }
        }

        /// <summary>
        /// Mount time
        /// </summary>
        public DateTime MountTime
        {
            get { return Helper.FsHelper.ConvertFromUnixTimestamp(MTime); }
            set { MTime = Helper.FsHelper.ConvertToUnixTimestamp(value); }
        }

        /// <summary>
        /// Write time, in seconds since the epoch.
        /// u32 s_wtime (0x30)
        /// </summary>
        public uint WTime
        {
            get { return ReadUInt32(0x2C); }
            set { WriteUInt32(0x2C, value); }
        }

        /// <summary>
        /// Write time
        /// </summary>
        public DateTime WriteTime
        {
            get { return Helper.FsHelper.ConvertFromUnixTimestamp(WTime); }
            set { WTime = Helper.FsHelper.ConvertToUnixTimestamp(value); }
        }

        /// <summary>
        /// Number of mounts since the last fsck.
        /// u16 s_mnt_count (0x34)
        /// </summary>
        public uint MountCount
        {
            get { return ReadUInt16(0x34); }
            set { WriteUInt16(0x34, value); }
        }

        /// <summary>
        /// Number of mounts beyond which a fsck is needed.
        /// u16 s_max_mnt_count (0x36)
        /// </summary>
        public uint MaxMountCount
        {
            get { return ReadUInt16(0x36); }
            set { WriteUInt16(0x36, value); }
        }

        /// <summary>
        /// Magic signature, 0xEF53
        /// u16 s_magic (0x38)
        /// </summary>
        public uint Magic
        {
            get { return ReadUInt16(0x38); }
            set { WriteUInt16(0x38, value); }
        }

        /// <summary>
        /// Is superblock magic correct
        /// </summary>
        public bool IsCorrectMagic => (Magic == 0xEF53u);

        /// <summary>
        /// File system state. Valid values are:
        ///   0x0001 Cleanly umounted
        ///   0x0002 Errors detected
        ///   0x0004 Orphans being recovered
        /// u16 s_state (0x3A)
        /// </summary>
        public uint State
        {
            get { return ReadUInt16(0x3A); }
            set { WriteUInt16(0x3A, value); }
        }


        /// <summary>
        /// Behaviour when detecting errors. One of:
        /// 1 Continue
        /// 2 Remount read-only
        /// 3 Panic
        /// u16 s_errors (0x3C)
        /// </summary>
        public uint Errors
        {
            get { return ReadUInt16(0x3C); }
            set { WriteUInt16(0x3C, value); }
        }

        /// <summary>
        /// Minor revision level.
        /// u16 s_minor_rev_level (0x3E)
        /// </summary>
        public uint MinorRevisionLevel
        {
            get { return ReadUInt16(0x3E); }
            set { WriteUInt16(0x3E, value); }
        }

        /// <summary>
        /// Time of last check, in seconds since the epoch.
        /// u32 s_lastcheck (0x40)
        /// </summary>
        public uint LastCheck
        {
            get { return ReadUInt32(0x40); }
            set { WriteUInt32(0x40, value); }
        }

        /// <summary>
        /// Lst check time
        /// </summary>
        public DateTime LastCheckTime
        {
            get { return Helper.FsHelper.ConvertFromUnixTimestamp(LastCheck); }
            set { LastCheck = Helper.FsHelper.ConvertToUnixTimestamp(value); }
        }

        /// <summary>
        /// Maximum time between checks, in seconds.
        /// u32 s_checkinterval	 (0x44)
        /// </summary>
        public uint CheckInterval
        {
            get { return ReadUInt32(0x44); }
            set { WriteUInt32(0x44, value); }
        }

        /// <summary>
        /// OS. One of:
        /// 0 Linux
        /// 1 Hurd
        /// 2 Masix
        /// 3 FreeBSD
        /// 4 Lites
        /// s_creator_os (0x48)
        /// </summary>
        public uint CreatorOS
        {
            get { return ReadUInt32(0x48); }
            set { WriteUInt32(0x48, value); }
        }

        /// <summary>
        /// Revision level. One of:
        /// 0 Original format
        /// 1 v2 format w/ dynamic inode size
        /// 
        /// u32 s_rev_level (0x4C)
        /// </summary>
        public uint RevLevel
        {
            get { return ReadUInt32(0x4C); }
            set { WriteUInt32(0x4C, value); }
        }

        /// <summary>
        /// Default uid for reserved blocks.
        /// s_def_resuid (0x50)
        /// </summary>
        public uint DefaultReservedUid
        {
            get { return ReadUInt16(0x50); }
            set { WriteUInt16(0x50, value); }
        }

        /// <summary>
        /// Default gid for reserved blocks.
        /// s_def_resgid (0x52)
        /// </summary>
        public uint DefaultReservedGid
        {
            get { return ReadUInt16(0x52); }
            set { WriteUInt16(0x52, value); }
        }

        // These fields are for EXT4_DYNAMIC_REV superblocks only.
        // Note: the difference between the compatible feature set and the incompatible feature set is that if there is a bit set in the incompatible 
        // feature set that the kernel doesn't know about, it should refuse to mount the filesystem.
        // e2fsck's requirements are more strict; if it doesn't know about a feature in either the compatible or incompatible feature set, 
        // it must abort and not try to meddle with things it doesn't understand...

        /// <summary>
        /// First non-reserved inode.
        /// s_first_ino (0x54)
        /// </summary>
        public uint FirstNonreservedINode
        {
            get { return ReadUInt32(0x54); }
            set { WriteUInt32(0x54, value); }
        }

        /// <summary>
        /// Size of inode structure, in bytes.
        /// s_inode_size (0x58)
        /// </summary>
        public uint INodeSize
        {
            get { return ReadUInt16(0x58); }
            set { WriteUInt16(0x58, value); }
        }

        /// <summary>
        /// Block group # of this superblock.
        /// s_block_group_nr (0x5A)
        /// </summary>
        public uint BlockGroup
        {
            get { return ReadUInt16(0x5A); }
            set { WriteUInt16(0x5A, value); }
        }

        /// <summary>
        /// Compatible feature set flags. Kernel can still read/write this fs even if it doesn't understand a flag; 
        /// e2fsck will not attempt to fix a filesystem with any unknown COMPAT flags. Any of:
        ///   0x1	Directory preallocation (COMPAT_DIR_PREALLOC).
        ///   0x2	"imagic inodes". Used by AFS to indicate inodes that are not linked into the directory namespace. 
        ///         Inodes marked with this flag will not be added to lost+found by e2fsck. (COMPAT_IMAGIC_INODES).
        ///   0x4	Has a journal (COMPAT_HAS_JOURNAL).
        ///   0x8	Supports extended attributes (COMPAT_EXT_ATTR).
        ///   0x10	Has reserved GDT blocks for filesystem expansion. Requires RO_COMPAT_SPARSE_SUPER. (COMPAT_RESIZE_INODE).
        ///   0x20	Has indexed directories. (COMPAT_DIR_INDEX).
        ///   0x40	"Lazy BG". Not in Linux kernel, seems to have been for uninitialized block groups? (COMPAT_LAZY_BG).
        ///   0x80	"Exclude inode". Intended for filesystem snapshot feature, but not used. (COMPAT_EXCLUDE_INODE).
        ///   0x100	"Exclude bitmap". Seems to be used to indicate the presence of snapshot-related exclude bitmaps? Not defined in kernel or used in e2fsprogs. (COMPAT_EXCLUDE_BITMAP).
        ///   0x200	Sparse Super Block, v2. If this flag is set, the SB field s_backup_bgs points to the two block groups that contain backup superblocks. (COMPAT_SPARSE_SUPER2).
        /// u32 s_feature_compat (0x5C	)
        /// </summary>
        public uint FeatureCompatible
        {
            get { return ReadUInt32(0x5C); }
            set { WriteUInt32(0x5C, value); }
        }

        /// <summary>
        /// Incompatible feature set. If the kernel or e2fsck doesn't understand one of these bits, it will refuse to mount or attempt to repair the filesystem. Any of:
        ///   0x1     Compression. Not implemented. (INCOMPAT_COMPRESSION).
        ///   0x2     Directory entries record the file type. See ext4_dir_entry_2 below. (INCOMPAT_FILETYPE).
        ///   0x4     Filesystem needs journal recovery. (INCOMPAT_RECOVER).
        ///   0x8     Filesystem has a separate journal device. (INCOMPAT_JOURNAL_DEV).
        ///   0x10    Meta block groups. See the earlier discussion of this feature. (INCOMPAT_META_BG).
        ///   0x40    Files in this filesystem use extents. (INCOMPAT_EXTENTS).
        ///   0x80    Enable a filesystem size over 2^32 blocks. (INCOMPAT_64BIT).
        ///   0x100   Multiple mount protection. Prevent multiple hosts from mounting the filesystem concurrently by updating a reserved block
        ///           periodically while mounted and checking this at mount time to determine if the filesystem is in use on another host. (INCOMPAT_MMP).
        ///   0x200   Flexible block groups. See the earlier discussion of this feature. (INCOMPAT_FLEX_BG).
        ///   0x400   Inodes can be used to store large extended attribute values (INCOMPAT_EA_INODE).
        ///   0x1000  Data in directory entry. Allow additional data fields to be stored in each dirent, after struct ext4_dirent. 
        ///           The presence of extra data is indicated by flags in the high bits of ext4_dirent file type flags (above EXT4_FT_MAX). The flag EXT4_DIRENT_LUFID = 0x10 is used to store a 128-bit File Identifier for Lustre. The flag EXT4_DIRENT_IO64 = 0x20 is used to store the high word of 64-bit inode numbers. Feature still in development. (INCOMPAT_DIRDATA).
        ///   0x2000  Metadata checksum seed is stored in the superblock. This feature enables the administrator to change the UUID of a metadata_csum filesystem 
        ///           while the filesystem is mounted; without it, the checksum definition requires all metadata blocks to be rewritten.
        ///           (INCOMPAT_CSUM_SEED).
        ///  0x4000   Large directory >2GB or 3-level htree. Prior to this feature, directories could not be larger than 4GiB 
        ///           and could not have an htree more than 2 levels deep. If this feature is enabled, directories can be larger than 4GiB and 
        ///           have a maximum htree depth of 3. (INCOMPAT_LARGEDIR).
        ///  0x8000   Data in inode. Small files or directories are stored directly in the inode i_blocks and/or xattr space. (INCOMPAT_INLINE_DATA).
        ///  0x10000  Encrypted inodes are present on the filesystem (INCOMPAT_ENCRYPT).
        /// u32 s_feature_incompat (0x60)
        /// </summary>
        public uint FeatureIncompatible
        {
            get { return ReadUInt32(0x60); }
            set { WriteUInt32(0x60, value); }
        }

        /// <summary>
        /// Readonly-compatible feature set. If the kernel doesn't understand one of these bits, it can still mount read-only, 
        /// but e2fsck will refuse to modify the filesystem. Any of:
        ///  0x1    Sparse superblocks. See the earlier discussion of this feature. (RO_COMPAT_SPARSE_SUPER).
        ///  0x2    Allow storing files larger than 2GiB (RO_COMPAT_LARGE_FILE).
        ///  0x4    Was intended for use with htree directories, but was not needed. Not used in kernel or e2fsprogs (RO_COMPAT_BTREE_DIR).
        ///  0x8    This filesystem has files whose space usage is stored in i_blocks in units of filesystem blocks, not 512-byte sectors. 
        ///         Inodes using this feature will be marked with EXT4_INODE_HUGE_FILE. (RO_COMPAT_HUGE_FILE)
        ///  0x10   Group descriptors have checksums. In addition to detecting corruption, this is useful for lazy formatting with uninitialized groups (RO_COMPAT_GDT_CSUM).
        ///  0x20   Indicates that the old ext3 32,000 subdirectory limit no longer applies. 
        ///         A directory's i_links_count will be set to 1 if it is incremented past 64,999. (RO_COMPAT_DIR_NLINK).
        ///  0x40   Indicates that large inodes exist on this filesystem, storing extra fields after EXT2_GOOD_OLD_INODE_SIZE. (RO_COMPAT_EXTRA_ISIZE).
        ///  0x80   This filesystem has a snapshot. Not implemented in ext4. (RO_COMPAT_HAS_SNAPSHOT).
        ///  0x100  Quota is handled transactionally with the journal (RO_COMPAT_QUOTA).
        ///  0x200  This filesystem supports "bigalloc", which means that filesystem block allocation bitmaps are tracked 
        ///         in units of clusters (of blocks) instead of blocks (RO_COMPAT_BIGALLOC).
        ///  0x400  This filesystem supports metadata checksumming. (RO_COMPAT_METADATA_CSUM; implies RO_COMPAT_GDT_CSUM, though GDT_CSUM must not be set)
        ///  0x800  Filesystem supports replicas. This feature is neither in the kernel nor e2fsprogs. (RO_COMPAT_REPLICA).
        ///  0x1000 Read-only filesystem image; the kernel will not mount this image read-write and most tools will refuse to write to the image. (RO_COMPAT_READONLY).
        ///  0x2000 Filesystem tracks project quotas. (RO_COMPAT_PROJECT)
        /// u32 s_feature_ro_compat (0x64)
        /// </summary>
        public uint FeatureReadonly
        {
            get { return ReadUInt32(0x64); }
            set { WriteUInt32(0x64, value); }
        }

        /// <summary>
        /// 128-bit UUID for volume.
        /// u8 s_uuid[16] (0x68)
        /// </summary>
        public byte[] UUID
        {
            get { return ReadArray(0x68, 0x10); }
            set
            {
                if (value == null) throw new ArgumentNullException("UUID must be not null value");
                if (value.Length != 0x10) throw new ArgumentException("UUID length must be 16 bytes");

                WriteArray(0x68, value, 0x10);
            }
        }

        /// <summary>
        /// Volume label.
        /// char s_volume_name[16] (0x78)
        /// </summary>
        public string VolumeName
        {
            get { return ReadString(0x78, 0x10); }
            set { WriteString(0x78, value, 0x10); }
        }

        /// <summary>
        /// Directory where filesystem was last mounted.
        /// char s_last_mounted[64] (0x88)
        /// </summary>
        public string LastMountedDir
        {
            get { return ReadString(0x88, 0x40); }
            set { WriteString(0x88, value, 0x40); }
        }

        /// <summary>
        /// For compression (Not used in e2fsprogs/Linux)
        /// s_algorithm_usage_bitmap (0xC8)
        /// </summary>
        public uint AlgorithmUsageBitmap
        {
            get { return ReadUInt32(0xC8); }
            set { WriteUInt32(0xC8, value); }
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

        //
        // 64bit support valid if EXT4_FEATURE_COMPAT_64BIT
        //

        /// <summary>
        /// High 32-bits of the block count.
        /// u32 s_blocks_count_hi (0x150) 
        /// </summary>
        uint BlocksCountHi
        {
            get { return ReadUInt32(0x150); }
            set { WriteUInt32(0x150, value); }
        }

        /// <summary>
        /// High 32-bits of the reserved block count.
        /// u32 s_r_blocks_count_hi (0x154) 
        /// </summary>
        uint ReservedBlocksCountHi
        {
            get { return ReadUInt32(0x154); }
            set { WriteUInt32(0x154, value); }
        }

        /// <summary>
        /// High 32-bits of the free block count.
        /// u32 s_free_blocks_count_hi (0x158) 
        /// </summary>
        uint FreeBlocksCountHi
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
