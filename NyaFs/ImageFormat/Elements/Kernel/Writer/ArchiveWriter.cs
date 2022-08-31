using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class ArchiveWriter : Writer
    {
        string Filename;
        byte[] PackedData = null;
        Types.CompressionType Compression;


        public ArchiveWriter(Types.CompressionType Compression)
        {
            this.Compression = Compression;
        }

        public ArchiveWriter(string Filename, Types.CompressionType Compression)
        {
            this.Filename = Filename;
            this.Compression = Compression;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            var Data = Helper.FitHelper.GetCompressedData(Kernel.Image, Compression);

            if (Filename != null)
            {
                PackedData = null;
                System.IO.File.WriteAllBytes(Filename, Data);
            }
            else
                PackedData = Data;
        }
    }
}
