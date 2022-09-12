using System;
using System.IO;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Gzip : BaseCompressor
    {
        internal Gzip()
        {

        }

        internal Gzip(byte[] Raw, long Offset) : base(Raw, Offset, 8)
        {

        }

        /// <summary>
        /// Should be in range 1…9 (inclusive). Defaults to 9.
        /// u32 compression_level (0x00)
        /// </summary>
        internal uint Level
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// Should be in range 8…15 (inclusive) Defaults to 15.
        /// u16 window_size (0x04)
        /// </summary>
        internal uint WindowSize
        {
            get { return ReadUInt16(4); }
            set { WriteUInt16(4, value); }
        }

        /// <summary>
        /// A bitfield describing the enabled strategies. If no flags are set, the default strategy is implicitly used. Flags:
        ///  0x01: Default
        ///  0x02: Filtered
        ///  0x04: Huffman only
        ///  0x08: Run Length Encoded
        ///  0x10: Fixed
        /// u16 strategies (0x06)
        /// </summary>
        internal EnabledStrategies Strategies
        {
            get { return (EnabledStrategies)ReadUInt16(6); }
            set { WriteUInt16(6, Convert.ToUInt32(value)); }
        }

        internal override byte[] Compress(byte[] data)
        {
            using (var uncompressedStream = new MemoryStream(data))
            using (var zipStream = new SharpCompress.Compressors.Deflate.ZlibStream(uncompressedStream, SharpCompress.Compressors.CompressionMode.Compress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        internal override byte[] Decompress(byte[] data)
        {
            
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new SharpCompress.Compressors.Deflate.ZlibStream(compressedStream, SharpCompress.Compressors.CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        [Flags]
        internal enum EnabledStrategies
        {
            Default = 0x01,
            Filtered = 0x02,
            HuffmanOnly = 0x04,
            RunLengthEncoded = 0x08,
            Fixed = 0x10
        }
    }
}
