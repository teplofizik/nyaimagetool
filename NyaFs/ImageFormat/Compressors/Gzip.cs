using CrcSharp;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Compressors
{
    static class Gzip
    {
        readonly static byte[] GzipHeader = new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A };

        static UInt32 CalcCrc(byte[] data)
        {
            var crc32 = new Crc(new CrcParameters(32, 0x04c11db7, 0xffffffff, 0xffffffff, true, true));

            return Convert.ToUInt32(crc32.CalculateAsNumeric(data));
        }

        public static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new DeflateStream(compressedStream, CompressionLevel.Optimal))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                var Compressed = compressedStream.ToArray();
                var Res = new byte[Compressed.Length + 8];
                Res.WriteArray(0, Compressed, Compressed.Length);
                Res.WriteUInt32(Compressed.Length, CalcCrc(data));
                Res.WriteUInt32(Compressed.Length + 4, Convert.ToUInt32(data.Length) & 0xFFFFFFFFU);

                return Res;
            }
        }

        public static byte[] CompressWithHeader(byte[] Data)
        {
            byte[] Compressed = Compress(Data);
            byte[] Res = new byte[Compressed.Length + GzipHeader.Length];

            Res.WriteArray(0, GzipHeader, GzipHeader.Length);
            Res.WriteArray(GzipHeader.Length, Compressed, Compressed.Length);

            return Res;
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
