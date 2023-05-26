using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class CImgReader : Reader
    {
        Types.CvImage Image;

        public CImgReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public CImgReader(byte[] Data)
        {
            Image = new Types.CvImage(Data);
        }

        /// <summary>
        /// Читаем в файловую систему 
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            if (Image.Correct)
            {
                byte[] Raw = Helper.FitHelper.GetDecompressedData(Image.Content, Types.CompressionType.IH_COMP_GZIP);

                if (Dst.Info.Type == Types.ImageType.IH_TYPE_INVALID)
                    Dst.Info.Type = Types.ImageType.IH_TYPE_RAMDISK;

                Dst.Info.Compression = Types.CompressionType.IH_COMP_GZIP;
                // TODO: detect fit or other...
                DetectAndRead(Dst, Raw);
            }
            else
                Log.Error(0, "Invalid CImg archive");
        }
    }
}
