using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class RawWriter : Writer
    {
        string Filename;

        public RawWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            System.IO.File.WriteAllBytes(Filename, Kernel.Image);
        }
    }
}
