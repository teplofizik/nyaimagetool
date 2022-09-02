using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    class SqInode : RawPacket
    {
        public SqInode(byte[] Data, long Size) : base(Data)
        {

        }

        public SqInode(byte[] Data) : this(Data, 0x10)
        {

        }

        /// <summary>
        /// INode size
        /// </summary>
        internal virtual long INodeSize => 0x10;

        /// <summary>
        /// The type of item described by the inode which follows this header.
        /// u16 inode_type (0x00)
        /// </summary>
        internal SqInodeType InodeType
        {
            get { return (SqInodeType)ReadUInt16(0x00); }
            set { WriteUInt16(0x00, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// A bitmask representing the permissions for the item described by the inode. The values match with the permission values of mode_t (the mode bits, not the file type)
        /// u16 permissions (0x02)
        /// </summary>
        internal uint Permissions
        {
            get { return ReadUInt16(0x02); }
            set { WriteUInt16(0x02, value); }
        }

        /// <summary>
        /// The index of the user id in the UID/GID Table
        /// u16 uid_idx (0x04)
        /// </summary>
        internal uint UidIndex
        {
            get { return ReadUInt16(0x04); }
            set { WriteUInt16(0x04, value); }
        }

        /// <summary>
        /// The index of the group  id in the UID/GID Table
        /// u16 gid_idx (0x06)
        /// </summary>
        internal uint GidIndex
        {
            get { return ReadUInt16(0x06); }
            set { WriteUInt16(0x06, value); }
        }

        /// <summary>
        /// The unsigned number of seconds (not counting leap seconds) since 00:00, Jan 1 1970 UTC when the item described by the inode was last modified
        /// u32 modified_time (0x08)
        /// </summary>
        internal uint ModifiedTime
        {
            get { return ReadUInt32(0x8); }
            set { WriteUInt32(0x8, value); }
        }

        /// <summary>
        /// The position of this inode in the full list of inodes. 
        /// Value should be in the range [1, inode_count](from the superblock) 
        /// This can be treated as a unique identifier for this inode, and can be used as a key to recreate hard links: 
        /// when processing the archive, remember the visited values of inode_number. 
        /// If an inode number has already been visited, this inode is hardlinked.
        /// u32 inode_number (0x0C)
        /// </summary>
        internal uint INodeNumber
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }
    }
}
