using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Types.zImage;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
{
    class zImageReader : Reader
    {
        private BasezImage Image;

        public zImageReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        public zImageReader(byte[] Raw)
        {
            Image = new BasezImage(Raw);
        }

        public override void ReadToKernel(LinuxKernel Dst)
        {
            if(Image.IsMagicCorrect)
            {
                Log.Write(0, $"zImage detected! Endian: {(Image.IsBigEndian ? "Big" : "Little")}");


                var Archive = Image.Archive;

                if (Archive != null)
                {
                    if (Image.Compression != Types.CompressionType.IH_COMP_NONE)
                    {
                        Log.Write(0, $"Found {Helper.FitHelper.GetCompression(Image.Compression)} archive!");
                        Dst.Info.Compression = Image.Compression;
                        Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;
                        Dst.Image = Helper.FitHelper.GetDecompressedData(Archive, Image.Compression);
                    }
                }
                else
                {
                    Log.Error(0, "Image not found!..");
                }
            }
        }
    }
}
