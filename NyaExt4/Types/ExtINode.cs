using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types
{
    internal class ExtINode : ArrayWrapper
    {
        /// <summary>
        /// Wrapper for INode struct
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        public ExtINode(byte[] Data, long Offset) : base(Data, Offset, 0x80) // ext2, ext3 => 128 bytes
        {

        }

        /// <summary>
        /// For extensions...
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        /// <param name="Size"></param>
        public ExtINode(byte[] Data, long Offset, long Size) : base(Data, Offset, Size)
        {

        }

        public override string ToString()
        {
            return $"t:{NodeType} m:{ModeStr:x04} u:{UID} g:{GID} s:{SizeLo} l:{LinksCount} f:{Flags}";
        }

        /// <summary>
        /// File mode. Any of:
        /// 0x1     S_IXOTH (Others may execute)
        /// 0x2     S_IWOTH(Others may write)
        /// 0x4     S_IROTH(Others may read)
        /// 0x8     S_IXGRP(Group members may execute)
        /// 0x10    S_IWGRP(Group members may write)
        /// 0x20    S_IRGRP(Group members may read)
        /// 0x40    S_IXUSR(Owner may execute)
        /// 0x80    S_IWUSR(Owner may write)
        /// 0x100   S_IRUSR(Owner may read)
        /// 0x200   S_ISVTX(Sticky bit)
        /// 0x400   S_ISGID(Set GID)
        /// 0x800   S_ISUID(Set UID)
        /// These are mutually-exclusive file types:
        /// 0x1000  S_IFIFO(FIFO)
        /// 0x2000  S_IFCHR(Character device)
        /// 0x4000  S_IFDIR(Directory)
        /// 0x6000  S_IFBLK(Block device)
        /// 0x8000  S_IFREG(Regular file)
        /// 0xA000  S_IFLNK(Symbolic link)
        /// 0xC000  S_IFSOCK(Socket)
        /// u16 i_mode (0x00)
        /// </summary>
        public uint Mode
        {
            get { return ReadUInt16(0x00); }
            set { WriteUInt16(0x00, value); }
        }

        /// <summary>
        /// 0x1000  S_IFIFO(FIFO)
        /// 0x2000  S_IFCHR(Character device)
        /// 0x4000  S_IFDIR(Directory)
        /// 0x6000  S_IFBLK(Block device)
        /// 0x8000  S_IFREG(Regular file)
        /// 0xA000  S_IFLNK(Symbolic link)
        /// 0xC000  S_IFSOCK(Socket)
        /// </summary>
        public ExtINodeType NodeType => (ExtINodeType)(Mode & 0xF000);

        /// <summary>
        /// Filesystem node type
        /// </summary>
        public FilesystemEntryType FsNodeType
        {
            get
            {
                switch(NodeType)
                {
                    case ExtINodeType.FIFO: return FilesystemEntryType.Fifo;
                    case ExtINodeType.CHAR: return FilesystemEntryType.Character;
                    case ExtINodeType.DIR: return FilesystemEntryType.Directory;
                    case ExtINodeType.BLOCK: return FilesystemEntryType.Block;
                    case ExtINodeType.REG: return FilesystemEntryType.Regular;
                    case ExtINodeType.LINK: return FilesystemEntryType.Link;
                    case ExtINodeType.SOCK: return FilesystemEntryType.Socket;
                    default: return FilesystemEntryType.Invalid;
                }
            }
        }

        /// <summary>
        /// Mode as string
        /// </summary>
        public string ModeStr => Helper.FsHelper.ConvertModeToString(Mode & 0xFFF);

        /// <summary>
        /// Lower 16-bits of Owner UID.
        /// u16 i_uid (0x02)
        /// </summary>
        public uint UID
        {
            get { return ReadUInt16(0x02); }
            set { WriteUInt16(0x02, value); }
        }

        /// <summary>
        /// Lower 32-bits of size in bytes.
        /// u32 i_size_lo (0x04)
        /// </summary>
        public uint SizeLo
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// Last access time, in seconds since the epoch. 
        /// However, if the EA_INODE inode flag is set, this inode stores an extended attribute value and this field contains the checksum of the value.
        /// u32 i_atime (0x08)
        /// </summary>
        public uint ATime
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// Last inode change time, in seconds since the epoch. 
        /// However, if the EA_INODE inode flag is set, this inode stores an extended attribute value and this field contains the lower 32 bits of the attribute value's reference count.
        /// u32 i_ctime (0xC)
        /// </summary>
        public uint CTime
        {
            get { return ReadUInt32(0xC); }
            set { WriteUInt32(0xC, value); }
        }

        /// <summary>
        /// Last data modification time, in seconds since the epoch. 
        /// However, if the EA_INODE inode flag is set, this inode stores an extended attribute value and this field contains the number of the inode that owns the extended attribute.
        /// u32 i_mtime (0x10)
        /// </summary>
        public uint MTime
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// Deletion Time, in seconds since the epoch.
        /// u32 i_dtime (0x14)
        /// </summary>
        public uint DTime
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Lower 16-bits of GID.
        /// u16 i_gid (0x18)
        /// </summary>
        public uint GID
        {
            get { return ReadUInt16(0x18); }
            set { WriteUInt16(0x18, value); }
        }

        /// <summary>
        /// Hard link count. Normally, ext4 does not permit an inode to have more than 65,000 hard links. This applies to files as well as directories, which means that there cannot be more than 64,998 subdirectories in a directory (each subdirectory's '..' entry counts as a hard link, as does the '.' entry in the directory itself). 
        /// With the DIR_NLINK feature enabled, ext4 supports more than 64,998 subdirectories by setting this field to 1 to indicate that the number of hard links is not known.
        /// u16 i_links_count (0x1A)
        /// </summary>
        public uint LinksCount
        {
            get { return ReadUInt16(0x1A); }
            set { WriteUInt16(0x1A, value); }
        }

        /// <summary>
        /// Lower 32-bits of "block" count. If the huge_file feature flag is not set on the filesystem, the file consumes i_blocks_lo 512-byte blocks on disk. If huge_file is set and EXT4_HUGE_FILE_FL is NOT set in inode.i_flags, then the file consumes i_blocks_lo + (i_blocks_hi << 32) 512-byte blocks on disk. 
        /// If huge_file is set and EXT4_HUGE_FILE_FL IS set in inode.i_flags, then this file consumes (i_blocks_lo + i_blocks_hi << 32) filesystem blocks on disk.
        /// u32 i_blocks_lo (0x1C)
        /// </summary>
        public uint BlocksLo
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// Inode flags. Any of:
        /// 0x1	This file requires secure deletion (EXT4_SECRM_FL). (not implemented)
        /// 0x2	This file should be preserved, should undeletion be desired(EXT4_UNRM_FL). (not implemented)
        /// 0x4	File is compressed(EXT4_COMPR_FL). (not really implemented)
        /// 0x8	All writes to the file must be synchronous(EXT4_SYNC_FL).
        /// 0x10	File is immutable(EXT4_IMMUTABLE_FL).
        /// 0x20	File can only be appended(EXT4_APPEND_FL).
        /// 0x40	The dump(1) utility should not dump this file(EXT4_NODUMP_FL).
        /// 0x80	Do not update access time(EXT4_NOATIME_FL).
        /// 0x100	Dirty compressed file(EXT4_DIRTY_FL). (not used)
        /// 0x200	File has one or more compressed clusters(EXT4_COMPRBLK_FL). (not used)
        /// 0x400	Do not compress file(EXT4_NOCOMPR_FL). (not used)
        /// 0x800	Encrypted inode(EXT4_ENCRYPT_FL). This bit value previously was EXT4_ECOMPR_FL(compression error), which was never used.
        /// 0x1000	Directory has hashed indexes (EXT4_INDEX_FL).
        /// 0x2000	AFS magic directory (EXT4_IMAGIC_FL).
        /// 0x4000	File data must always be written through the journal (EXT4_JOURNAL_DATA_FL).
        /// 0x8000	File tail should not be merged (EXT4_NOTAIL_FL). (not used by ext4)
        /// 0x10000	All directory entry data should be written synchronously(see dirsync) (EXT4_DIRSYNC_FL).
        /// 0x20000	Top of directory hierarchy(EXT4_TOPDIR_FL).
        /// 0x40000	This is a huge file(EXT4_HUGE_FILE_FL).
        /// 0x80000	Inode uses extents(EXT4_EXTENTS_FL).
        /// 0x200000	Inode stores a large extended attribute value in its data blocks(EXT4_EA_INODE_FL).
        /// 0x400000	This file has blocks allocated past EOF(EXT4_EOFBLOCKS_FL). (deprecated)
        /// 0x01000000	Inode is a snapshot(EXT4_SNAPFILE_FL). (not in mainline)
        /// 0x04000000	Snapshot is being deleted(EXT4_SNAPFILE_DELETED_FL). (not in mainline)
        /// 0x08000000	Snapshot shrink has completed(EXT4_SNAPFILE_SHRUNK_FL). (not in mainline)
        /// 0x10000000	Inode has inline data(EXT4_INLINE_DATA_FL).
        /// 0x20000000	Create children with the same project ID(EXT4_PROJINHERIT_FL).
        /// 0x80000000	Reserved for ext4 library(EXT4_RESERVED_FL).
        /// Aggregate flags:
        /// 0x4BDFFF	User-visible flags.
        /// 0x4B80FF	User-modifiable flags. Note that while EXT4_JOURNAL_DATA_FL and EXT4_EXTENTS_FL can be set with setattr, they are not in the kernel's EXT4_FL_USER_MODIFIABLE mask, since it needs to handle the setting of these flags in a special manner and they are masked out of the set of flags that are saved directly to i_flags.
        /// u32 i_flags (0x20)
        /// </summary>
        public uint Flags
        {
            get { return ReadUInt32(0x20); }
            set { WriteUInt32(0x20, value); }
        }

        /// <summary>
        /// Inode version. However, if the EA_INODE inode flag is set, this inode stores an extended attribute value and this field contains the upper 32 bits of the attribute value's reference count.
        /// (linux)
        /// __le32	l_i_version	(0x24)
        /// </summary>
        public uint INodeVersion
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }

        private string LinkText => ((NodeType == ExtINodeType.LINK) && (SizeLo <= 60)) ? UTF8Encoding.UTF8.GetString(BlockRaw) : "";

        /// <summary>
        /// It NodeType == LINK and Data Length < 60 bytes, text contains in blocks field.
        /// </summary>
        public byte[] BlockRaw
        {
            get { return ReadArray(0x28, SizeLo); }
            set { WriteArray(0x28, value, SizeLo); }
        }

        /// <summary>
        /// i_block[EXT4_N_BLOCKS=15] (0x28)
        /// </summary>
        public uint[] Block
        {
            get { return ReadUInt32Array(0x28, 15); }
            set
            {
                if (value == null) throw new ArgumentNullException("IMode Block must be not null value");
                if (value.Length != 0x10) throw new ArgumentException("IMode Block length must be 15 dwords");

                for(int i = 0; i < 15; i++)
                    WriteUInt32(0x28 + i * 4, value[i]); 
            }
        }

        /// <summary>
        /// File version (for NFS).
        /// __le32	i_generation (0x64)
        /// </summary>
        public uint Generation
        {
            get { return ReadUInt32(0x64); }
            set { WriteUInt32(0x64, value); }
        }

        /// <summary>
        /// Lower 32-bits of extended attribute block. ACLs are of course one of many possible extended attributes; I think the name of this field is a result of the first use of extended attributes being for ACLs.
        /// __le32	i_file_acl_lo (0x68)
        /// </summary>
        public uint FileACLLo
        {
            get { return ReadUInt32(0x68); }
            set { WriteUInt32(0x68, value); }
        }

        /// <summary>
        /// Upper 32-bits of file/directory size. In ext2/3 this field was named i_dir_acl, though it was usually set to zero and never used.
        /// __le32	i_size_high  (0x6C)
        /// </summary>
        public uint SizeHi
        {
            get { return ReadUInt32(0x6C); }
            set { WriteUInt32(0x6C, value); }
        }

        /// <summary>
        /// Upper 16-bits of the block count. Please see the note attached to i_blocks_lo.
        /// u16 l_i_blocks_high (0x74)
        /// </summary>
        public uint BlocksHi
        {
            get { return ReadUInt16(0x74); }
            set { WriteUInt16(0x74, value); }
        }


        /// <summary>
        /// Upper 16-bits of the extended attribute block (historically, the file ACL location). See the Extended Attributes section below.
        /// __le32	l_i_file_acl_high (0x76)
        /// </summary>
        public uint FileACLHi
        {
            get { return ReadUInt32(0x76); }
            set { WriteUInt32(0x76, value); }
        }

        /// <summary>
        /// Upper 16-bits of the Owner UID.
        /// u16 l_i_uid_high (0x78)
        /// </summary>
        public uint UIDHi
        {
            get { return ReadUInt16(0x78); }
            set { WriteUInt16(0x78, value); }
        }

        /// <summary>
        /// Upper 16-bits of the GID.
        /// u16 l_i_gid_high (0x7A)
        /// </summary>
        public uint GIDHi
        {
            get { return ReadUInt16(0x7A); }
            set { WriteUInt16(0x7A, value); }
        }

        /// <summary>
        /// Lower 16-bits of the inode checksum.
        /// u16 l_i_checksum_lo (0x7C)
        /// </summary>
        public uint ChecksumLo
        {
            get { return ReadUInt16(0x7C); }
            set { WriteUInt16(0x7C, value); }
        }
    }
}
