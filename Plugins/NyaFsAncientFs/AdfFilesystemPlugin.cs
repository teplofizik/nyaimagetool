using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only Amiga OFS/FFS (.adf) filesystem plugin.</summary>
    public class AdfFilesystemPlugin : FilesystemPlugin
    {
        public AdfFilesystemPlugin() : base("fs.adf", "adf", true, true, false)
        {
        }

        public override string DisplayName => "Amiga OFS/FFS";

        public override string[] Aliases => new[] { "amiga", "ofs", "ffs" };

        public override string[] Extensions => new[] { "adf" };

        public override bool DetectFilesystem(ImageData Image) =>
            AdfReader.IsAdf(Image.Raw);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new AdfReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }

        public override NyaFs.ImageFormat.Elements.Fs.Writer.Writer GetFilesystemWriter(
            NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs) => new AdfWriter();
    }
}
