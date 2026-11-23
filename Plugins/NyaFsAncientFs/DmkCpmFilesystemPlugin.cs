using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only TRS-80 DMK image with a CP/M filesystem.</summary>
    public class DmkCpmFilesystemPlugin : FilesystemPlugin
    {
        public DmkCpmFilesystemPlugin() : base("fs.dmk-cpm", "dmk-cpm", true, false, false)
        {
        }

        public override string DisplayName => "TRS-80 DMK (CP/M)";

        public override string[] Aliases => new[] { "dmk", "trs80", "trs-80", "trs80-cpm" };

        public override string[] Extensions => new[] { "dmk" };

        public override bool DetectFilesystem(ImageData Image) =>
            DmkReader.TryDetect(Image.Raw, Image.SourceName, out _);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new DmkReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
