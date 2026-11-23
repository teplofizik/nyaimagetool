using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only Atari DOS 2.0/2.5 (.atr) filesystem plugin.</summary>
    public class AtariDosFilesystemPlugin : FilesystemPlugin
    {
        public AtariDosFilesystemPlugin() : base("fs.atari", "atari", true, true, false)
        {
        }

        public override string DisplayName => "Atari DOS 2.x";

        public override string[] Aliases => new[] { "atr", "dos2", "dos25", "atari8" };

        public override string[] Extensions => new[] { "atr" };

        public override bool DetectFilesystem(ImageData Image) =>
            AtariDosReader.IsAtariDos(Image.Raw, Image.SourceName);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new AtariDosReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }

        public override NyaFs.ImageFormat.Elements.Fs.Writer.Writer GetFilesystemWriter(
            NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs) => new AtariDosWriter();
    }
}
