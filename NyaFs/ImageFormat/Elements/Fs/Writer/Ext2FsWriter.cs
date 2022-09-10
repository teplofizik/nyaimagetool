using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class Ext2FsWriter : BaseFsWriter
    {
        // TODO: FS size detector
        public Ext2FsWriter(uint DiskSize) : base(new Filesystem.Ext2.Ext2FsBuilder(DiskSize)) { }

        public Ext2FsWriter(uint DiskSize, string Filename) : base(new Filesystem.Ext2.Ext2FsBuilder(DiskSize), Filename) { }
    }
}
