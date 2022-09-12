using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    // https://dr-emann.github.io/squashfs/
    internal class SqSuperblock : ArrayWrapper
    {
        public SqSuperblock() : base(new byte[0x60], 0, 0x60)
        {
            Fill(0);
            Magic = 0x73717368;
            ModificationTime = Universal.Helper.FsHelper.ConvertToUnixTimestamp(DateTime.Now);
            BlockSize = 0x20000;
            BlockLog = 0x11;
            Flags = SqSuperblockFlags.DUPLICATES;

            VersionMajor = 0x04;
            VersionMinor = 0x00;
        }

        public SqSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x60)
        {

        }

        /// <summary>
        /// Must match the value of 0x73717368 to be considered a squashfs archive
        /// u32 magic (0x00)
        /// </summary>
        public uint Magic
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, value); }
        }

        /// <summary>
        /// Is magic field correct
        /// </summary>
        public bool IsMagicCorrect => (Magic == 0x73717368u);

        /// <summary>
        /// The number of inodes stored in the inode table
        /// u32 inode_count (0x04)
        /// </summary>
        public uint INodeCount
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// The number of seconds (not counting leap seconds) since 00:00, Jan 1 1970 UTC when the archive was created (or last appended to). 
        /// This is unsigned, so it expires in the year 2106 (as opposed to 2038).
        /// u32 modification_time (0x08)
        /// </summary>
        public uint ModificationTime
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// The size of a data block in bytes. Must be a power of two between 4096 and 1048576 (1 MiB)
        /// u32 block_size (0x0C)
        /// </summary>
        public uint BlockSize
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }

        /// <summary>
        /// The number of entries in the fragment table
        /// u32 fragment_entry_count (0x10)
        /// </summary>
        public uint FragmentEntryCount
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// Compression ID: 1 - GZIP, 2 - LZMA, 3 - LZO, 4 - XZ, 5 - LZ4, 6 - ZSTD
        /// u16 compression_id (0x14)
        /// </summary>
        public SqCompressionType CompressionId
        {
            get { return (SqCompressionType)ReadUInt16(0x14); }
            set { WriteUInt16(0x14, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// The log2 of block_size. If block_size and block_log do not agree, the archive is considered corrupt
        /// u16 block_log (0x16)
        /// </summary>
        public uint BlockLog
        {
            get { return ReadUInt16(0x16); }
            set { WriteUInt16(0x16, value); }
        }

        /// <summary>
        /// Superblock Flags
        /// u16 flags (0x18)
        /// </summary>
        public SqSuperblockFlags Flags
        {
            get { return (SqSuperblockFlags)ReadUInt16(0x18); }
            set { WriteUInt16(0x18, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// The number of entries in the id lookup table
        /// u16 id_count (0x1A)
        /// </summary>
        public uint IdCount
        {
            get { return ReadUInt16(0x1A); }
            set { WriteUInt16(0x1A, value); }
        }

        /// <summary>
        /// The major version of the squashfs file format. Should always equal 4
        /// u16 version_major (0x1C)
        /// </summary>
        public uint VersionMajor
        {
            get { return ReadUInt16(0x1C); }
            set { WriteUInt16(0x1C, value); }
        }

        /// <summary>
        /// The minor version of the squashfs file format. Should always equal 0
        /// u16 version_minor (0x1E)
        /// </summary>
        public uint VersionMinor
        {
            get { return ReadUInt16(0x1E); }
            set { WriteUInt16(0x1E, value); }
        }

        /// <summary>
        /// A reference to the inode of the root directory of the archive
        /// u64 root_inode_ref (0x20)
        /// </summary>
        public SqMetadataRef RootINodeRef
        {
            get { return new SqMetadataRef((long)ReadUInt64(0x20)); }
            set { WriteUInt64(0x20, (ulong)value.Value); }
        }

        /// <summary>
        /// The number of bytes used by the archive. Because squashfs archives are often padded to 4KiB, this can often be less than the file size
        /// u64 bytes_used (0x28)
        /// </summary>
        public ulong BytesUsed
        {
            get { return ReadUInt64(0x28); }
            set { WriteUInt64(0x28, value); }
        }

        /// <summary>
        /// The byte offset at which the id table starts
        /// u64 id_table_start (0x30)
        /// </summary>
        public ulong IdTableStart
        {
            get { return ReadUInt64(0x30); }
            set { WriteUInt64(0x30, value); }
        }

        /// <summary>
        /// The byte offset at which the xattr id table starts
        /// u64 xattr_id_table_start (0x38)
        /// </summary>
        public ulong XAttrIdTableStart
        {
            get { return ReadUInt64(0x38); }
            set { WriteUInt64(0x38, value); }
        }

        /// <summary>
        /// The byte offset at which the inode table starts
        /// u64 inode_table_start (0x40)
        /// </summary>
        public ulong INodeTableStart
        {
            get { return ReadUInt64(0x40); }
            set { WriteUInt64(0x40, value); }
        }

        /// <summary>
        /// The byte offset at which the directory table starts
        /// u64 directory_table_start (0x48)
        /// </summary>
        public ulong DirectoryTableStart
        {
            get { return ReadUInt64(0x48); }
            set { WriteUInt64(0x48, value); }
        }

        /// <summary>
        /// The byte offset at which the fragment table starts
        /// u64 fragment_table_start (0x50)
        /// </summary>
        public ulong FragmentTableStart
        {
            get { return ReadUInt64(0x50); }
            set { WriteUInt64(0x50, value); }
        }

        /// <summary>
        /// The byte offset at which the export table starts
        /// u64 export_table_start (0x58)
        /// </summary>
        public ulong ExportTableStart
        {
            get { return ReadUInt64(0x58); }
            set { WriteUInt64(0x58, value); }
        }

        /// <summary>
        /// Is header correct?
        /// </summary>
        public bool IsCorrect => IsMagicCorrect && (VersionMajor == 4) && (VersionMinor == 0);
    }
}
