using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only Macintosh File System (MFS, 1984) plugin.</summary>
    public class MfsFilesystemPlugin : FilesystemPlugin
    {
        public MfsFilesystemPlugin() : base("fs.mfs", "mfs", true, false, false)
        {
        }

        public override string DisplayName => "MFS (Macintosh)";

        public override string[] Aliases => new[] { "mfs1", "macintosh" };

        public override string[] Extensions => new[] { "mfs", "dsk", "img" };

        public override bool DetectFilesystem(ImageData Image) =>
            MfsReader.IsMfs(Image.Raw);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new MfsReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
