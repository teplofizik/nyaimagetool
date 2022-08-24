using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class GzWriter : Writer
    {
        string Filename;
        byte[] PackedData = null;

        public GzWriter()
        {

        }

        public GzWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            var Data = Compressors.Gzip.CompressWithHeader(Kernel.Image);

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
