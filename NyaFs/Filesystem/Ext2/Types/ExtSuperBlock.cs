﻿using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Ext2.Types
{
    /// <summary>
    /// The Super Block of Ext Filesystem
    /// https://ext4.wiki.kernel.org/index.php/Ext4_Disk_Layout
    /// </summary>
    internal class ExtSuperBlock : ArrayWrapper
    {
        public ExtSuperBlock(byte[] Data, long Offset) : base(Data, Offset, 0x400)
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
            get { return Universal.Helper.FsHelper.ConvertFromUnixTimestamp(MTime); }
            set { MTime = Universal.Helper.FsHelper.ConvertToUnixTimestamp(value); }
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
            get { return Universal.Helper.FsHelper.ConvertFromUnixTimestamp(WTime); }
            set { WTime = Universal.Helper.FsHelper.ConvertToUnixTimestamp(value); }
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
            get { return Universal.Helper.FsHelper.ConvertFromUnixTimestamp(LastCheck); }
            set { LastCheck = Universal.Helper.FsHelper.ConvertToUnixTimestamp(value); }
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
    }
}
