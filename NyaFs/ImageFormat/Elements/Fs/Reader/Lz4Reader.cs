using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class Lz4Reader : Reader
    {
        byte[] Data = null;

        public Lz4Reader(string Filename)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
        }

        public Lz4Reader(byte[] Data)
        {
            this.Data = Data;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(Filesystem Dst)
        {
            byte[] Raw = Compressors.Lz4.Decompress(Data);

            Dst.Info.Compression = Types.CompressionType.IH_COMP_LZ4;
            DetectAndRead(Dst, Raw);
        }
    }
}
