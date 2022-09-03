using CrcSharp;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Compressors
{
    static class ZStd
    {
        public static byte[] Decompress(byte[] Data)
        {
            using var decompressor = new ZstdSharp.Decompressor();
            return decompressor.Unwrap(Data).ToArray();
        }
    }
}
