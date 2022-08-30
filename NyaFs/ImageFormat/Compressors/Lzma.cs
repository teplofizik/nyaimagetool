using CrcSharp;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Compressors
{
    static class Lzma
    {
        public static byte[] Decompress(byte[] data)
        {
            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

            var properties = data.ReadArray(0, 5);
            long outSize = (long)data.ReadUInt64(5);
            long inSize = data.Length - 13;

            decoder.SetDecoderProperties(properties);
            using (var input = new MemoryStream(data.ReadArray(13, inSize)))
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
