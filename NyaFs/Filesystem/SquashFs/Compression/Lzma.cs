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
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();

            long outSize = Data.Length;
            long inSize = Data.Length - 13;

            using (var input = new MemoryStream(Data))
            {
                using (var output = new MemoryStream())
                {
                    byte[] Size = new byte[8];
                    Size.WriteUInt64(0, Convert.ToUInt64(Data.Length));

                    encoder.WriteCoderProperties(output);
                    output.Write(Size);
                    encoder.Code(input, output, input.Length, outSize, null);
                    return output.ToArray();
                }
            }
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
