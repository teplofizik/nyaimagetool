using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class LzmaReader : Reader
    {
        byte[] Data = null;

        public LzmaReader(string Filename)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
        }

        public LzmaReader(byte[] Data)
        {
            this.Data = Data;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(Filesystem Dst)
        {
            byte[] Raw = Compressors.Lzma.Decompress(Data);

            Dst.Info.Compression = Types.CompressionType.IH_COMP_LZMA;
            DetectAndRead(Dst, Raw);
        }
    }
}
