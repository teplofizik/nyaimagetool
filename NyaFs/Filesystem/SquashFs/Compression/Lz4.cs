using System;
using System.IO;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Lz4 : BaseCompressor
    {
        internal Lz4()
        {

        }

        internal Lz4(byte[] Raw, long Offset) : base(Raw, Offset, 8)
        {

        }

        /// <summary>
        /// The only supported value is 1 (LZ4_LEGACY)
        /// u32 version (0x00)
        /// </summary>
        internal uint Version
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// A bitfield describing the enabled LZ4 flags. There is currently only one possible flag:
        ///  0x01: Use LZ4 High Compression(HC) mode
        /// u32 flags (0x04)
        /// </summary>
        internal Lz4Flags Flags
        {
            get { return (Lz4Flags)ReadUInt32(4); }
            set { WriteUInt32(4, Convert.ToUInt32(value)); }
        }

        internal override byte[] Compress(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] Decompress(byte[] Data)
        {
            var Res = new byte[8192];

            FT.LZ4.LZ4Codec.Decode(Data, 0, Data.Length, Res, 0, Res.Length);
            return Res;
        }

        [Flags]
        internal enum Lz4Flags
        {
            HighCompressionMode = 0x01
        }
    }
}
