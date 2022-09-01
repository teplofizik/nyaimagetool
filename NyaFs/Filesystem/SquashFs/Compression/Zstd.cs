using System;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Zstd : BaseCompressor
    {
        internal Zstd()
        {

        }

        internal Zstd(byte[] Raw, long Offset) : base(Raw, Offset, 8)
        {

        }

        /// <summary>
        /// Should be in range 1..22 (inclusive). The real maximum is the zstd defined ZSTD_maxCLevel()
        /// u32 compression_level (0x00)
        /// </summary>
        internal uint Level
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        internal override byte[] Compress(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] Decompress(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
