using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class Lz4Writer : Writer
    {
        string Filename;
        byte[] PackedData = null;

        public Lz4Writer()
        {

        }

        public Lz4Writer(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            var Data = Compressors.Lz4.CompressWithHeader(Kernel.Image);

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
