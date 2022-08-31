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
        private static byte[] GetCompressionHeader(int CompressionLevel, long Length)
        {
            // 6D 00 00 80 00 [u64 Length]
            var Header = new byte[13];
            Header.WriteByte(0, 0x6D);
            Header.WriteByte(3, 0x80);
            Header.WriteUInt64(5, (ulong)Length);
            return Header;
        }

        public static byte[] CompressWithHeader(byte[] Data)
        {
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();

            using (var input = new MemoryStream(Data))
            {
                using (var output = new MemoryStream())
                {
                    encoder.WriteCoderProperties(output);

                    encoder.Code(input, output, input.Length, -1, null);
                    return output.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] Data)
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
