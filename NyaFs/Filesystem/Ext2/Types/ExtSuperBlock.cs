using Extension.Packet;
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
            get { return ReadUInt32(0x30); }
            set { WriteUInt32(0x30, value); }
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

        // ext2: 0x68 74 CC 73 3B 
        // ext2: 0x6c 6C 44 44 AE 
        // ext2: 0x70 AE 20 23 9F 
        // ext2: 0x74 0F 21 C0 CB
    }
}
