using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types.zImage
{
    class BasezImage : RawPacket
    {
        long ArchiveOffset;

        public BasezImage(byte[] Raw) : base(Raw)
        {
            ArchiveOffset = FindArchiveOffset();
        }

        /// <summary>
        /// 18 28 6F 01
        /// </summary>
        public uint Magic => ReadUInt32BE(0x24);

        /// <summary>
        /// Endianess
        /// </summary>
        public uint Endian => ReadUInt32(0x28);

        /// <summary>
        /// Is Magic correct
        /// </summary>
        public bool IsMagicCorrect => (Magic == 0x18286F01u) || (Magic == 0x016F2818u);

        /// <summary>
        /// Is big endian arch
        /// </summary>
        public bool IsBigEndian => (Magic == 0x016F2818u);

        /// <summary>
        /// Type of compression
        /// </summary>
        public Types.CompressionType Compression = CompressionType.IH_COMP_NONE;

        private long FindArchiveOffset()
        {
            for(long i = 0; i < getLength() / 2; i++)
            {
                var C = Raw[i];
                if (C == 0x1f)
                {
                    // Check: is gz header?
                    var Magic = ReadUInt32BE(i);
                    if ((Magic & 0xffffff00u) == 0x1f8b0800u)
                    {
                        Compression = CompressionType.IH_COMP_GZIP;
                        return i;
                    }
                }
                if (C == 0xFD)
                {
                    var Magic = ReadUInt32BE(i);
                    if (Magic == 0xFD377A58u)
                    {
                        Compression = CompressionType.IH_COMP_XZ;
                        return i;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Start of image offset
        /// </summary>
        public uint Start => ReadUInt32(0x28);
        /// <summary>
        /// End of image offset
        /// </summary>
        public uint End => ReadUInt32(0x2c);

        public byte[] Boot
        {
            get
            {
                if (ArchiveOffset > 0)
                    return ReadArray(0, ArchiveOffset);
                else
                    return null;
            }
        }

        public byte[] Archive
        {
            get
            {
                if (ArchiveOffset > 0)
                {
                    return ReadArray(ArchiveOffset, getLength() - ArchiveOffset);
                }
                else
                    return null;
            }
        }
    }
}
