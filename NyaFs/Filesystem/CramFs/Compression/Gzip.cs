using System;
using System.IO;

namespace NyaFs.Filesystem.CramFs.Compression
{
    internal static class Gzip
    {
        internal static byte[] Compress(byte[] data)
        {
            using (var uncompressedStream = new MemoryStream(data))
            using (var zipStream = new SharpCompress.Compressors.Deflate.ZlibStream(uncompressedStream, SharpCompress.Compressors.CompressionMode.Compress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        internal static byte[] Decompress(byte[] data)
        {
            
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new SharpCompress.Compressors.Deflate.ZlibStream(compressedStream, SharpCompress.Compressors.CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

    }
}
