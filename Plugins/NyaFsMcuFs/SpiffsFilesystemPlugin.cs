using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.ImageFormat.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsMcuFs
{
    /// <summary>
    /// SPIFFS filesystem plugin (read). Detection is heuristic (no magic in the format).
    /// </summary>
    public class SpiffsFilesystemPlugin : FilesystemPlugin
    {
        public SpiffsFilesystemPlugin() : base("fs.spiffs", "spiffs", true, true, false) { }

        public override bool DetectFilesystem(ImageData Image)
        {
            if (Image.Length < 8192) return false;
            return SpiffsReader.LooksLikeSpiffs(Image.ReadArray(0, Image.Length));
        }

        public override Reader GetFilesystemReader(byte[] Data)
        {
            var R = new SpiffsReader(Data);
            return new BaseFsReader(FsType.Spiffs, R, Data.Length);
        }

        public override NyaFs.ImageFormat.Elements.Fs.Writer.Writer GetFilesystemWriter(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs)
        {
            return new NyaFs.ImageFormat.Elements.Fs.Writer.BaseFsWriter(new SpiffsBuilder(Fs.FsSize));
        }
    }
}
