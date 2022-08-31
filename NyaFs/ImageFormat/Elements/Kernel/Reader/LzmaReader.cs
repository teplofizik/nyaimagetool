using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
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

        public override void ReadToKernel(LinuxKernel Dst)
        {
            byte[] Raw = Compressors.Lzma.Decompress(Data);

            Dst.Image = Raw;
            Dst.Info.Compression = Types.CompressionType.IH_COMP_LZMA;
            Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;
            Helper.LogHelper.KernelInfo(Dst);
        }
    }
}
