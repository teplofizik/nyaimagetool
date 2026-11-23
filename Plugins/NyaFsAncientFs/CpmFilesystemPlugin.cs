using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>
    /// External plugin for ancient filesystems. Currently provides read-only CP/M.
    /// </summary>
    public class CpmFilesystemPlugin : FilesystemPlugin
    {
        public CpmFilesystemPlugin() : base("fs.cpm", "cpm", true, true, false)
        {
        }

        public override string DisplayName => "CP/M";

        public override string[] Aliases => new[] { "cpm22", "cpm2", "cpm3" };

        public override string[] Extensions => new[] { "dsk", "cpm" };

        public override bool DetectFilesystem(ImageData Image) =>
            CpmReader.TryDetect(Image.Raw, Image.SourceName, out _);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new CpmReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }

        public override NyaFs.ImageFormat.Elements.Fs.Writer.Writer GetFilesystemWriter(
            NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs) => new CpmDosWriter();
    }
}
