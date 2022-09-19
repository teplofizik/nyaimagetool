using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class RomFsWriter : BaseFsWriter
    {
        public RomFsWriter() : base(new Filesystem.RomFs.RomFsBuilder()) { }

        public RomFsWriter(string Filename) : base(new Filesystem.RomFs.RomFsBuilder(), Filename) { }
    }
}
