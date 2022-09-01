using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal abstract class BaseCompressor : ArrayWrapper
    {
        protected readonly bool HasMetadata;

        internal BaseCompressor() : base(null, 0, 0)
        {
            HasMetadata = false;
        }

        internal BaseCompressor(byte[] Raw, long Offset, long Size) : base(Raw, Offset, Size)
        {
            HasMetadata = true;
        }

        internal abstract byte[] Compress(byte[] Data);

        internal abstract byte[] Decompress(byte[] Data);
    }
}
