using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class GzReader : Reader
    {
        byte[] Data = null;

        public GzReader(string Filename)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
        }

        public GzReader(byte[] Data)
        {
            this.Data = Data;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(Filesystem Dst)
        {
            byte[] Raw = Compressors.Gzip.Decompress(Data);

            var Fs = FilesystemDetector.DetectFs(Raw);
            switch (Fs)
            {
                case Types.FsType.Cpio:
                    {
                        var Reader = new CpioReader(Raw);
                        Reader.ReadToFs(Dst);
                    } 
                    break;
                case Types.FsType.Ext2:
                    {
                        var Reader = new ExtReader(Raw);
                        Reader.ReadToFs(Dst);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Unknown filesystem");
            }
        }
    }
}
