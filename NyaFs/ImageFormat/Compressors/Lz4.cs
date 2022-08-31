using CrcSharp;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Compressors
{
    static class Lz4
    {
        public static byte[] CompressWithHeader(byte[] Data)
        {
            var descriptor = new FT.LZ4.LZ4Descriptor(null, false, false, false, null, 0x400000);

            using (var output = new MemoryStream())
            {
                using (var encoded = new FT.LZ4.LZ4EncoderStream(output, descriptor, sett => FT.LZ4.LZ4Encoder.Create(sett.Chaining, FT.LZ4.LZ4Level.L10_OPT, sett.BlockSize)))
                {
                    encoded.Write(Data);
                    encoded.Close();

                    return output.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] Data)
        {
            using (var inputraw = new MemoryStream(Data))
            {
                using (var input = new FT.LZ4.LZ4DecoderStream(inputraw, sett => FT.LZ4.LZ4Decoder.Create(sett.Chaining, sett.BlockSize)))
                {
                    using (var output = new MemoryStream())
                    {
                        input.CopyTo(output);

                        return output.ToArray();
                    }
                }
            }
        }
    }
}
