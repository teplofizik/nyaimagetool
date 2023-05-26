using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
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

        public override void ReadToKernel(LinuxKernel Dst)
        {
            if (Image.Correct)
            {
                byte[] Raw = Helper.FitHelper.GetDecompressedData(Image.Content, Types.CompressionType.IH_COMP_GZIP);

                if (Dst.Info.Type == Types.ImageType.IH_TYPE_INVALID)
                    Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;

                Dst.Info.Compression = Types.CompressionType.IH_COMP_GZIP;

                Dst.Image = Raw;
            }
            else
                Log.Error(0, "Invalid CImg archive");
        }
    }
}
