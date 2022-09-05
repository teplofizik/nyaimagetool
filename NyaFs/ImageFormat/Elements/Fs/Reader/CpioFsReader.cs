using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class CpioFsReader : BaseFsReader
    {
        public CpioFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public CpioFsReader(byte[] Data) : base(Types.FsType.Cpio, new Filesystem.Cpio.CpioFsReader(Data)) { }
    }
}
