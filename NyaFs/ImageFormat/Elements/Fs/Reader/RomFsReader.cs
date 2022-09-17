using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class RomFsReader : BaseFsReader
    {
        public RomFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public RomFsReader(byte[] Data) : base(Types.FsType.RomFs, new Filesystem.RomFs.RomFsReader(Data)) { }
    }
}
