using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class Writer
    {
        public virtual void WriteKernel(LinuxKernel Kernel)
        {

        }

        public virtual bool HasRawStreamData => RawStream != null;

        public virtual byte[] RawStream => null;

    }
}
