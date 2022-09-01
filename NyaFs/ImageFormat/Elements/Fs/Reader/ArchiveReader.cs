using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class ArchiveReader : Reader
    {
        byte[] Data = null;
        Types.CompressionType Compression;

        public ArchiveReader(string Filename, Types.CompressionType Compression)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
            this.Compression = Compression;
        }

        public ArchiveReader(byte[] Data)
        {
            this.Data = Data;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            byte[] Raw = Helper.FitHelper.GetDecompressedData(Data, Compression);

            Dst.Info.Compression = Compression;
            DetectAndRead(Dst, Raw);
        }
    }
}
