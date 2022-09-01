using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    [Flags]
    internal enum SqSuperblockFlags
    {
        UNCOMPRESSED_INODES = 0x0001, // Inodes are stored uncompressed. For backward compatibility reasons, UID/GIDs are also stored uncompressed.
        UNCOMPRESSED_DATA  = 0x0002, // Data are stored uncompressed
        CHECK = 0x0004, // Unused in squashfs 4+. Should always be unset
        UNCOMPRESSED_FRAGMENTS = 0x0008, // Fragments are stored uncompressed
        NO_FRAGMENTS = 0x0010, // Fragments are not used. Files smaller than the block size are stored in a full block.
        ALWAYS_FRAGMENTS = 0x0020, // If the last block of a file is smaller than the block size, it will be instead stored as a fragment
        DUPLICATES = 0x0040, // Identical files are recognized, and stored only once
        EXPORTABLE = 0x0080, // Filesystem has support for export via NFS (The export table is populated)
        UNCOMPRESSED_XATTRS = 0x0100, // Xattrs are stored uncompressed
        NO_XATTRS = 0x0200, //  Xattrs are not stored
        COMPRESSOR_OPTIONS = 0x0400, // The compression options section is present
        UNCOMPRESSED_IDS = 0x0800 // UID/GIDs are stored uncompressed. Note that the UNCOMPRESSED_INODES flag also has this effect. If that flag is set, this flag has no effect. This flag is currently only available on master in git, no released version of squashfs yet supports it.
    }
}
