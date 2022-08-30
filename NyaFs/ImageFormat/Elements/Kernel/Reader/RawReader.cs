using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
{
    class RawReader : Reader
    {
        byte[] Data = null;

        public RawReader(string Filename)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
        }

        public RawReader(byte[] Data)
        {
            this.Data = Data;
        }

        public override void ReadToKernel(LinuxKernel Dst)
        {
            Dst.Image = Data;
            Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;
            Helper.LogHelper.KernelInfo(Dst);
        }
    }
}
