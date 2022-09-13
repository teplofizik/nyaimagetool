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

        // https://github.com/oleg-st/ZstdSharp

        internal override byte[] Compress(byte[] Data)
        {
            using var compressor = new ZstdSharp.Compressor();
            return compressor.Wrap(Data).ToArray();
        }

        internal override byte[] Decompress(byte[] Data)
        {
            using var decompressor = new ZstdSharp.Decompressor();
            return decompressor.Unwrap(Data).ToArray();
        }
    }
}
