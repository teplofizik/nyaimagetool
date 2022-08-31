using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
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

        public override void ReadToKernel(LinuxKernel Dst)
        {
            byte[] Raw = Compressors.Lz4.Decompress(Data);

            Dst.Image = Raw;
            Dst.Info.Compression = Types.CompressionType.IH_COMP_LZ4;
            Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;
            Helper.LogHelper.KernelInfo(Dst);
        }
    }
}
