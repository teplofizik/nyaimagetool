using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class CramFsReader : BaseFsReader
    {
        public CramFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public CramFsReader(byte[] Data) : base(Types.FsType.CramFs, new Filesystem.CramFs.CramFsReader(Data)) { }
    }
}
