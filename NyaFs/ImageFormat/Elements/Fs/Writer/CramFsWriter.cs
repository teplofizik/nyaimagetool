using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class CramFsWriter : BaseFsWriter
    {
        public CramFsWriter() : base(new Filesystem.CramFs.CramFsBuilder()) { }

        public CramFsWriter(string Filename) : base(new Filesystem.CramFs.CramFsBuilder(), Filename) { }
    }
}
