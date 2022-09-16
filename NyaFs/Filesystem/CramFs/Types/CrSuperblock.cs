using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Types
{
    class CrSuperblock : ArrayWrapper
    {
        public CrSuperblock(byte[] Data, long Offset) : base(Data, Offset, 0x40)
        {

        }

        /// <summary>
        /// CramFS Magic value: 0x28cd3d45
        /// </summary>
        public uint Magic
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// Is Magic correct
        /// </summary>
        public bool IsMagicCorrect => (Magic == 0x28cd3d45u) && (Signature == "Compressed ROMFS");

        /// <summary>
        /// CramFS size
        /// </summary>
        public uint Size
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, value); }
        }

        /// <summary>
        /// CramFS flags
        /// </summary>
        public uint Flags
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint Future
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }

        /// <summary>
        /// Signature: "Compressed ROMFS"
        /// </summary>
        public string Signature
        {
            get { return ReadString(0x10, 0x10); }
            set { WriteString(0x10, value, 0x10); }
        }

        /// <summary>
        /// fsid.crc
        /// </summary>
        public uint FsIDCrc
        {
            get { return ReadUInt32(0x20); }
            set { WriteUInt32(0x20, value); }
        }

        /// <summary>
        /// fsid.edition 
        /// </summary>
        public uint FsIDEdition
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }

        /// <summary>
        /// fsid.blocks 
        /// </summary>
        public uint FsIDBlocks
        {
            get { return ReadUInt32(0x28); }
            set { WriteUInt32(0x28, value); }
        }

        /// <summary>
        /// fsid.files 
        /// </summary>
        public uint FsIDFiles
        {
            get { return ReadUInt32(0x2C); }
            set { WriteUInt32(0x2C, value); }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return ReadString(0x30, 0x10); }
            set { WriteString(0x30, value, 0x10); }
        }

    }
}
