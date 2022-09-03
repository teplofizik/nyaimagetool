using System;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class ExtReader : BaseFsReader
    {
        public ExtReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        public ExtReader(byte[] data) : base(new Filesystem.Ext2.Ext2FsReader(data)) { }
    }
}