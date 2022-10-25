using Extension.Array;
using NyaFs.ImageFormat.Elements.Fs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.FilesystemPlugins
{
    class RomFsFilesystemPlugin : Base.FilesystemPlugin
    {
        public RomFsFilesystemPlugin() : base("fs.romfs", "romfs", true, true, true)
        {

        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectFilesystem(byte[] Image) => (Image.ReadUInt32BE(0) == 0x2d726f6du);

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => 
            new Elements.Fs.Reader.BaseFsReader(Types.FsType.RomFs, new Filesystem.RomFs.RomFsReader(Data));

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public override Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) =>
            new Elements.Fs.Writer.BaseFsWriter(new Filesystem.RomFs.RomFsBuilder());

        public override bool CheckFilesystem(LinuxFilesystem Fs)
        {
            List<uint> InvalidGid = new List<uint>();
            List<uint> InvalidUid = new List<uint>();

            IterateAllFilesystem(Fs, I =>
            {
                if (I.Group > 0)
                    InvalidGid.Add(I.Group);
                if (I.User > 0)
                    InvalidUid.Add(I.User);
            });

            if (InvalidGid.Count > 0)
            {
                Log.Error(0, $"Filesystem containg group id which cannot be saved in romfs image: {String.Join(", ", Array.ConvertAll(InvalidGid.ToArray(), G => $"{G}"))}");
                return false;
            }
            else if (InvalidUid.Count > 0)
            {
                Log.Error(0, $"Filesystem containg user id which cannot be saved in romfs image: {String.Join(", ", Array.ConvertAll(InvalidUid.ToArray(), G => $"{G}"))}");
                return false;
            }
            else
                return true;
        }
    }
}
