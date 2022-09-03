using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.ImageFormat.Types.Android
{
    class LegacyAndroidImage : RawPacket
    {
        public LegacyAndroidImage(byte[] Raw) : base(Raw)
        {

        }

        public string Magic
        {
            get { return ReadString(0, 8); }
            set { WriteString(0, value, 8); }
        }

        public bool IsMagicCorrect => Magic == "ANDROID!";

        /// <summary>
        /// Kernel size in bytes 
        /// </summary>
        public uint KernelSize
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// Kernel address
        /// </summary>
        public uint KernelAddress
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }

        /// <summary>
        /// Ramdisk size in bytes 
        /// </summary>
        public uint RamdiskSize
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// Ramdisk address 
        /// </summary>
        public uint RamdiskAddress
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Second size in bytes 
        /// </summary>
        public uint SecondSize
        {
            get { return ReadUInt32(0x18); }
            set { WriteUInt32(0x18, value); }
        }

        /// <summary>
        /// Second address 
        /// </summary>
        public uint SecondAddress
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// Kernel tags address 
        /// </summary>
        public uint TagsAddress
        {
            get { return ReadUInt32(0x20); }
            set { WriteUInt32(0x20, value); }
        }

        /// <summary>
        /// Flash page size 
        /// </summary>
        public uint PageSize
        {
            get { return ReadUInt32(0x24); }
            set { WriteUInt32(0x24, value); }
        }


        /// <summary>
        /// Header format version (v0: unused = 0)
        /// </summary>
        public uint HeaderVersion
        {
            get { return ReadUInt32(0x28); }
            set { WriteUInt32(0x28, value); }
        }

        /// <summary>
        /// OS version 
        /// </summary>
        public uint OSVersion
        {
            get { return ReadUInt32(0x2C); }
            set { WriteUInt32(0x2C, value); }
        }

        /// <summary>
        /// Image name 
        /// </summary>
        public string Name
        {
            get { return ReadString(0x30, 0x10); }
            set { WriteString(0x30, value, 0x10); }
        }

        /// <summary>
        /// Boot args 
        /// </summary>
        public string BootAgrs
        {
            get { return ReadString(0x40, 0x200); }
            set { WriteString(0x40, value, 0x200); }
        }

        /// <summary>
        /// Image id: timestamp / checksum / sha hashes etc
        /// </summary>
        public uint[] Id
        {
            get { return ReadUInt32Array(0x240, 8); }
            set
            {
                for (int i = 0; i < 8; i++)
                    WriteUInt32(0x240 + i * 4, value[i]);
            }
        }

        /// <summary>
        /// Extra args 
        /// </summary>
        public string ExtraAgrs
        {
            get { return ReadString(0x260, 0x400); }
            set { WriteString(0x260, value, 0x400); }
        }

        public virtual long HeaderSize => 0x670;

        public uint KernelBase => KernelAddress - 0x8000;

        private long KernelOffset => HeaderSize.GetAligned(PageSize);
        private long RamdiskOffset => (KernelOffset + KernelSize).GetAligned(PageSize);

        public byte[] Kernel => ReadArray(KernelOffset, KernelSize);
        public byte[] Ramdisk => ReadArray(RamdiskOffset, RamdiskSize);

        public HashType DetectedHashType
        {
            get
            {
                var Id = this.Id;
                var ZeroCount = Id.Count(I => I == 0);
                switch (ZeroCount)
                {
                    case 0: return HashType.Sha256;
                    case 3: return HashType.Sha1;
                    case 8: return HashType.None;
                    default: return HashType.Unknown;
                }
            }
        }

        public enum HashType
        {
            Unknown,
            None,
            Sha1,
            Sha256
        }
    }
}
