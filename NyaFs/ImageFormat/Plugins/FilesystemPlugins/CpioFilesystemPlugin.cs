using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.FilesystemPlugins
{
    class CpioFilesystemPlugin : Base.FilesystemPlugin
    {
        public CpioFilesystemPlugin() : base("fs.cpio", "cpio", true, true, false)
        {

        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectFilesystem(byte[] Image) => Image.ReadUInt32BE(0) == 0x30373037u;

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => 
            new Elements.Fs.Reader.BaseFsReader(Types.FsType.Cpio, new Filesystem.Cpio.CpioFsReader(Data));

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) =>
            new Elements.Fs.Writer.BaseFsWriter(new Filesystem.Cpio.CpioFsBuilder());
    }
}
