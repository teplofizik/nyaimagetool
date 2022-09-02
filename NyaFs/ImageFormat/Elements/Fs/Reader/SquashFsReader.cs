using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class SquashFsReader : BaseFsReader
    {
        public SquashFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public SquashFsReader(byte[] Data) : base(new Filesystem.SquashFs.SquashFs(Data)) { }
    }
}
