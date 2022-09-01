using System;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Lzma : BaseCompressor
    {
        internal Lzma()
        {

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
