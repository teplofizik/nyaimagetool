using System.IO;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class ExtReader : BaseFsReader
    {
        public ExtReader(string Filename) : this(File.ReadAllBytes(Filename)) { }

        public ExtReader(byte[] data) : base("Ext2", new Filesystem.Ext2.Ext2FsReader(data)) { }
    }
}