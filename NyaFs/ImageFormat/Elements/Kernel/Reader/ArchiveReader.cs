using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
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

        public override void ReadToKernel(LinuxKernel Dst)
        {
            byte[] Raw = Helper.FitHelper.GetDecompressedData(Data, Compression);

            Dst.Image = Raw;
            Dst.Info.Compression = Compression;
            Dst.Info.Type = Types.ImageType.IH_TYPE_KERNEL;
        }
    }
}
