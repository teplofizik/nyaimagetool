using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.FilesystemPlugins
{
    class SquashFsFilesystemPlugin : Base.FilesystemPlugin
    {
        public SquashFsFilesystemPlugin() : base("fs.squashfs", "squashfs", true, true, true)
        {

        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectFilesystem(byte[] Image) => (Image.ReadUInt32BE(0) == 0x68737173);

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => 
            new Elements.Fs.Reader.BaseFsReader(Types.FsType.SquashFs, new Filesystem.Ext2.Ext2FsReader(Data));

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) =>
            new Elements.Fs.Writer.BaseFsWriter(new Filesystem.SquashFs.SquashFsBuilder(Fs.SquashFsCompression));
    }
}
