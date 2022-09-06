using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class CpioFsWriter : BaseFsWriter
    {
        public CpioFsWriter() : base(new Filesystem.Cpio.CpioFsBuilder()) { }

        public CpioFsWriter(string Filename) : base(new Filesystem.Cpio.CpioFsBuilder(), Filename) { }
    }
}
