using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Types
{
    class RmNode : ArrayWrapper
    {
        public RmNode(byte[] Data, long Offset) : base(Data, Offset, 0xC)
        {

        }

        /// <summary>
        /// The offset of the next file header (zero if no more files)
        /// </summary>
        public uint NextHeader
        {
            get { return ReadUInt32BE(0x00) & 0xFFFFFFF0u; }
            set { WriteUInt32BE(0x00, (ReadUInt32BE(0x00) & 0xF) | (value & 0xFFFFFFF0u)); }
        }

        /// <summary>
        /// File type
        /// </summary>
        public uint FileType
        {
            get { return ReadUInt32BE(0x00) & 0x7u; }
            set { WriteUInt32BE(0x00, (ReadUInt32BE(0x00) & 0xFFFFFFF0u) | (value & 0x07u)); }
        }

        /// <summary>
        /// Filesystem node type
        /// </summary>
        public Universal.Types.FilesystemItemType FsNodeType
        {
            set
            {
                switch (value)
                {
                    case Universal.Types.FilesystemItemType.HardLink: FileType = 0; break;
                    case Universal.Types.FilesystemItemType.Directory: FileType = 1; break;
                    case Universal.Types.FilesystemItemType.File: FileType = 2; break;
                    case Universal.Types.FilesystemItemType.SymLink: FileType = 3; break;
                    case Universal.Types.FilesystemItemType.Block: FileType = 4; break;
                    case Universal.Types.FilesystemItemType.Character: FileType = 5; break;
                    case Universal.Types.FilesystemItemType.Socket: FileType = 6; break;
                    case Universal.Types.FilesystemItemType.Fifo: FileType = 7; break;
                }
            }

            get
            {
                switch(FileType)
                {
                    case 0: return Universal.Types.FilesystemItemType.HardLink;
                    case 1: return Universal.Types.FilesystemItemType.Directory;
                    case 2: return Universal.Types.FilesystemItemType.File;
                    case 3: return Universal.Types.FilesystemItemType.SymLink;
                    case 4: return Universal.Types.FilesystemItemType.Block;
                    case 5: return Universal.Types.FilesystemItemType.Character;
                    case 6: return Universal.Types.FilesystemItemType.Socket;
                    case 7: return Universal.Types.FilesystemItemType.Fifo;
                    default: 
                        throw new NotImplementedException("Invalid state!");
                }
            }
        }


        /// <summary>
        /// File type
        /// </summary>
        public bool IsExecutable
        {
            get { return (ReadUInt32BE(0x00) & 0x8u) != 0; }
            set { WriteUInt32BE(0x00, (ReadUInt32BE(0x00) & 0xFFFFFFF7u) | (value ? 0x80u : 0x00u)); }
        }

        /// <summary>
        ///  Unix mode. Only owner can access file.
        /// </summary>
        public uint Mode
        {
            get
            {
                uint Res = 0x04 | 0x20 | 0x180; // rw-r--r--
                if (IsExecutable) Res |= 0x40; // --x-----
                return Res;
            }
        }

        /// <summary>
        /// Info for directories/hard links/devices
        /// </summary>
        public uint SpecInfo
        {
            get { return ReadUInt32BE(0x04); }
            set { WriteUInt32BE(0x04, value); }
        }

        /// <summary>
        /// The size of this file in bytes
        /// </summary>
        public uint Size
        {
            get { return ReadUInt32BE(0x08); }
            set { WriteUInt32BE(0x08, value); }
        }

        /// <summary>
        /// Covering the meta data, including the file name, and padding
        /// </summary>
        public uint Checksum
        {
            get { return ReadUInt32BE(0x0C); }
            set { WriteUInt32BE(0x0C, value); }
        }

        /// <summary>
        /// Node header size
        /// </summary>
        public uint HeaderSize => 0x10 + FilenameLength;

        /// <summary>
        /// Length of filename
        /// </summary>
        private uint FilenameLength
        {
            get
            {
                for (uint i = 15; i < 256; i += 16)
                {
                    if (ReadByte(0x10 + i) == 0)
                        return i + 1;
                }
                return 16;
            }
        }

        /// <summary>
        /// The zero terminated name of the volume, padded to 16 byte boundary.
        /// </summary>
        public string Name
        {
            get { return ReadString(0x10, FilenameLength); }
            set { WriteString(0x10, value, FilenameLength); }
        }
    }
}
