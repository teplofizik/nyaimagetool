using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only UNIX V6 (PDP-11) filesystem plugin.</summary>
    public class V6FsFilesystemPlugin : FilesystemPlugin
    {
        public V6FsFilesystemPlugin() : base("fs.v6", "v6", true, false, false)
        {
        }

        public override string DisplayName => "UNIX V6 (PDP-11)";

        public override string[] Aliases => new[] { "unix6", "v6fs", "pdp11" };

        public override string[] Extensions => new[] { "rx50", "v6" };

        public override bool DetectFilesystem(ImageData Image) =>
            V6FsReader.IsV6(Image.Raw);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new V6FsReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
