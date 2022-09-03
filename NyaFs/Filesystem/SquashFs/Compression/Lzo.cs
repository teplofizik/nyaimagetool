using System;
using System.IO;
using System.IO.Compression;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Lzo : BaseCompressor
    {
        private uint BlockSize;

        internal Lzo(uint BlockSize)
        {
            this.BlockSize = BlockSize;
        }

        internal Lzo(uint BlockSize, byte[] Raw, long Offset) : base(Raw, Offset, 8)
        {
            this.BlockSize = BlockSize;
        }

        /// <summary>
        /// Which variant of LZO to use (default is lzo1x_999):
        /// u32 algorithm (0x00)
        /// </summary>
        internal LzoAlgorithm Flags
        {
            get { return (LzoAlgorithm)ReadUInt32(4); }
            set { WriteUInt32(4, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Compression level. For lzo1x_999, this can be a value between 0 and 9 (defaults to 8). Has to be 0 for all other algorithms.
        /// u32 level (0x04)
        /// </summary>
        internal uint Level
        {
            get { return ReadUInt32(4); }
            set { WriteUInt32(4, value); }
        }

        internal override byte[] Compress(byte[] Data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] Decompress(byte[] Data)
        {
            return NyaLZO.LZO1xDecompressor.Decompress(Data, BlockSize);
        }

        internal enum LzoAlgorithm
        {
            lzo1x_1 = 0,
            lzo1x_1_11 = 1,
            lzo1x_1_12 = 2,
            lzo1x_1_15 = 3,
            lzo1x_999 = 4
        }
    }
}
