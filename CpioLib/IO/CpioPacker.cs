using CpioLib.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extension.Array;
using System.IO.Compression;

namespace CpioLib.IO
{
    public static class CpioPacker
    {
        public static void Save(CpioArchive Archive, string FileName)
        {
            File.WriteAllBytes(FileName, GetRawData(Archive));
        }

        public static void SaveGz(CpioArchive Archive, string FileName)
        {
            File.WriteAllBytes(FileName, Compress(GetRawData(Archive)));
        }

        public static byte[] GetRawData(CpioArchive Archive)
        {
            var Res = new List<byte>();

            foreach (var F in Archive.Files)
                Res.AddRange(F.getPacket());

            if (Archive.Trailer != null)
            {
                Res.AddRange(Archive.Trailer.getPacket());
            }

            var Padding = Convert.ToInt64(Res.Count).MakeSizeAligned(0x100);
            for (long i = 0; i < Padding; i++) Res.Add(0);
            return Res.ToArray();
        }

        static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                var Compressed = compressedStream.ToArray();

                return Compressed;
            }
        }

    }
}
