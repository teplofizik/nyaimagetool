using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    class SqDirectoryEntry : ArrayWrapper
    {
        private uint InodeBase;
        private long Block;

        private static int GetDirEntrySize(string Name) => 0x08 + Name.Length;

        public SqDirectoryEntry(uint InodeBase, long Block, SqInodeType Type, uint Offset, uint InodeOffset, string Filename) : base(new byte[GetDirEntrySize(Filename)], 0, GetDirEntrySize(Filename))
        {
            this.InodeBase = InodeBase;
            this.Block = Block;

            this.Offset = Offset;
            this.InodeOffset = InodeOffset;
            this.Type = Type;

            NameSize = Convert.ToUInt32(Filename.Length - 1);

            WriteString(0x08, Filename, Filename.Length);
        }

        public SqDirectoryEntry(uint InodeBase, long Block, byte[] Data, long Offset) : base(Data, Offset, 0x08 + 1 + Data.ReadUInt16(Offset + 6))
        {
            this.InodeBase = InodeBase;
            this.Block = Block;
        }

        /// <summary>
        /// An offset into the uncompressed inode metadata block
        /// u16 offset (0x00)
        /// </summary>
        internal uint Offset
        {
            get { return ReadUInt16(0x00); }
            set { WriteUInt16(0x00, value); }
        }

        /// <summary>
        /// The difference of this inode's number to the reference stored in the header
        /// u16 inode offset (0x02)
        /// </summary>
        internal uint InodeOffset
        {
            get { return ReadUInt16(0x02); }
            set { WriteUInt16(0x02, value); }
        }

        /// <summary>
        /// The inode type. For extended inodes, the corresponding basic type is stored here instead
        /// u16 type (0x06)
        /// </summary>
        internal SqInodeType Type
        {
            get { return (SqInodeType)ReadUInt16(0x04); }
            set { WriteUInt16(0x04, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Universal linux filesystem entry type
        /// </summary>
        internal Universal.Types.FilesystemItemType FsNodeType
        {
            get
            {
                switch(Type)
                {
                    case SqInodeType.ExtendedDirectory:
                    case SqInodeType.BasicDirectory: return Universal.Types.FilesystemItemType.Directory;
                    case SqInodeType.ExtendedFile:
                    case SqInodeType.BasicFile: return Universal.Types.FilesystemItemType.File;
                    case SqInodeType.BasicSymlink:
                    case SqInodeType.ExtendedSymlink: return Universal.Types.FilesystemItemType.SymLink;
                    case SqInodeType.BasicBlockDevice: 
                    case SqInodeType.ExtendedBlockDevice: return Universal.Types.FilesystemItemType.Block;
                    case SqInodeType.BasicCharDevice:
                    case SqInodeType.ExtendedCharDevice: return Universal.Types.FilesystemItemType.Character;
                    case SqInodeType.BasicFifo:
                    case SqInodeType.ExtendedFifo: return Universal.Types.FilesystemItemType.Fifo;
                    case SqInodeType.BasicSocket:
                    case SqInodeType.ExtendedSocket: return Universal.Types.FilesystemItemType.Socket;
                    default: return Universal.Types.FilesystemItemType.Unknown;
                }
            }
        }

        /// <summary>
        /// One less than the size of the entry name
        /// u16 name_size (0x06)
        /// </summary>
        internal uint NameSize
        {
            get { return ReadUInt16(0x06); }
            set { WriteUInt16(0x06, value); }
        }

        /// <summary>
        /// INode number
        /// </summary>
        internal uint Inode => InodeBase + InodeOffset;

        /// <summary>
        /// Name of first entry
        /// char[name_size+1] name (0x08)
        /// </summary>
        internal string Name => ReadString(0x08, NameSize + 1);

        /// <summary>
        /// Reference for INode
        /// </summary>
        internal SqMetadataRef Reference => new SqMetadataRef(Block, Offset);
    }
}
