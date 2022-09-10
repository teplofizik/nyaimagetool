using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class Ext2FsWriter : BaseFsWriter
    {
        // TODO: FS size detector
        public Ext2FsWriter() : base(new Filesystem.Ext2.Ext2FsBuilder(0x2000000)) { }

        public Ext2FsWriter(string Filename) : base(new Filesystem.Ext2.Ext2FsBuilder(0x2000000), Filename) { }
    }
}
