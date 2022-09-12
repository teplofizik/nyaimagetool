using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class SquashFsWriter : BaseFsWriter
    {
        public SquashFsWriter(Types.CompressionType Compression) : base(new Filesystem.SquashFs.SquashFsBuilder(Compression)) { }

        public SquashFsWriter(Types.CompressionType Compression, string Filename) : base(new Filesystem.SquashFs.SquashFsBuilder(Compression), Filename) { }
    }
}
