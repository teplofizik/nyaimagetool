using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class SquashFsWriter : BaseFsWriter
    {
        public SquashFsWriter(Filesystem.SquashFs.Types.SqCompressionType Compression) : base(new Filesystem.SquashFs.SquashFsBuilder(Compression)) { }

        public SquashFsWriter(Filesystem.SquashFs.Types.SqCompressionType Compression, string Filename) : base(new Filesystem.SquashFs.SquashFsBuilder(Compression), Filename) { }
    }
}
