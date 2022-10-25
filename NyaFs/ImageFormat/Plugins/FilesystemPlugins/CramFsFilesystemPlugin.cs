using Extension.Array;
using NyaFs.ImageFormat.Elements.Fs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.FilesystemPlugins
{
    class CramFsFilesystemPlugin : Base.FilesystemPlugin
    {
        public CramFsFilesystemPlugin() : base("fs.cramfs", "cramfs", true, true, true)
        {

        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectFilesystem(byte[] Image) => (Image.ReadUInt32BE(0) == 0x453dcd28u);

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => 
            new Elements.Fs.Reader.BaseFsReader(Types.FsType.CramFs, new Filesystem.CramFs.CramFsReader(Data));

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) =>
            new Elements.Fs.Writer.BaseFsWriter(new Filesystem.CramFs.CramFsBuilder());

        public override bool CheckFilesystem(LinuxFilesystem Fs)
        {
            List<uint> InvalidGid = new List<uint>();

            IterateAllFilesystem(Fs, I =>
            {
                if (I.Group > 255)
                    InvalidGid.Add(I.Group);
            });

            if (InvalidGid.Count > 0)
            {
                Log.Error(0, $"Filesystem containg group id which cannot be saved in cramfs image: {String.Join(", ", Array.ConvertAll(InvalidGid.ToArray(), G => $"{G}"))}");
                return false;
            }
            else
                return true;
        }
    }
}
