using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only TRS-80 TRSDOS/LDOS image inside a DMK container.</summary>
    public class TrsDosFilesystemPlugin : FilesystemPlugin
    {
        public TrsDosFilesystemPlugin() : base("fs.trsdos", "trsdos", true, false, false)
        {
        }

        public override string DisplayName => "TRS-80 TRSDOS/LDOS (DMK)";

        public override string[] Aliases => new[] { "ldos", "lsdos", "newdos", "trs80-trsdos" };

        public override string[] Extensions => new[] { "dmk" };

        public override bool DetectFilesystem(ImageData Image) =>
            TrsDosReader.TryDetect(Image.Raw, Image.SourceName);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new TrsDosReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
