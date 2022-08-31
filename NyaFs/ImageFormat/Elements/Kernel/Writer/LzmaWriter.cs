using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class LzmaWriter : Writer
    {
        string Filename;
        byte[] PackedData = null;

        public LzmaWriter()
        {

        }

        public LzmaWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            var Data = Compressors.Lzma.CompressWithHeader(Kernel.Image);

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
