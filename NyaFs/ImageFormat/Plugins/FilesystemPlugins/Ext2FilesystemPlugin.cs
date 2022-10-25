using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.FilesystemPlugins
{
    class Ext2FilesystemPlugin : Base.FilesystemPlugin
    {
        public Ext2FilesystemPlugin() : base("fs.ext2", "ext2", true, true, false)
        {

        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectFilesystem(byte[] Image) => (Image.ReadUInt16(0x438) == 0xEF53);

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => 
            new Elements.Fs.Reader.BaseFsReader(Types.FsType.Ext2, new Filesystem.Ext2.Ext2FsReader(Data));

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) =>
            new Elements.Fs.Writer.BaseFsWriter(new Filesystem.Ext2.Ext2FsBuilder(DetectFixDiskSize(Fs)));

        private uint DetectFixDiskSize(Elements.Fs.LinuxFilesystem Fs) => Convert.ToUInt32(Fs.GetContentSize() * 1.5).GetAligned(0x800000);
    }
}
