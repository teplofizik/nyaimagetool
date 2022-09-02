using Extension.Array;
using System;
using System.IO;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Lzma : BaseCompressor
    {
        internal Lzma()
        {

        }

        internal override byte[] Compress(byte[] Data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] Decompress(byte[] Data)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            decoder.SetDecoderProperties(Data.ReadArray(0, 5));

            long outSize = (long)Data.ReadUInt64(5);
            long inSize = Data.Length - 13;

            using (var input = new MemoryStream(Data.ReadArray(13, inSize)))
            {
                using (var output = new MemoryStream())
                {
                    decoder.Code(input, output, input.Length, outSize, null);
                    return output.ToArray();
                }
            }
        }
    }
}
