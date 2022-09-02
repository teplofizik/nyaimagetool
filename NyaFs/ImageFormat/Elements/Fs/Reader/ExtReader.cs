using System;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class ExtReader : BaseFsReader
    {
        Filesystem.Ext2.Ext2Fs Fs;

        public ExtReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        public ExtReader(byte[] data) : base(new Filesystem.Ext2.Ext2Fs(data)) { }
    }
}