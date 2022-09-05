using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
{
    class AndroidReader : Reader
    {
        Types.Android.LegacyAndroidImage Image;

        public AndroidReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }
        public AndroidReader(byte[] Raw)
        {
            Image = new Types.Android.LegacyAndroidImage(Raw);
        }

        public override void ReadToKernel(LinuxKernel Dst)
        {
            if(Image.IsMagicCorrect)
            {
                Dst.Image = Image.Kernel;
                Dst.Info.Compression = Types.CompressionType.IH_COMP_NONE;
                Dst.Info.DataLoadAddress = Image.KernelAddress;
                Dst.Info.EntryPointAddress = Image.KernelAddress;
            }
        }
    }
}
