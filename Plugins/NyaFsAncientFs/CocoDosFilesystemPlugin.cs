using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only Tandy Color Computer Disk BASIC (.dsk) plugin.</summary>
    public class CocoDosFilesystemPlugin : FilesystemPlugin
    {
        public CocoDosFilesystemPlugin() : base("fs.cocodos", "cocodos", true, false, false)
        {
        }

        public override string DisplayName => "Tandy CoCo Disk BASIC";

        public override string[] Aliases => new[] { "coco", "decb", "rsdos" };

        public override string[] Extensions => new[] { "dsk" };

        public override bool DetectFilesystem(ImageData Image) =>
            CocoDosReader.TryDetect(Image.Raw, Image.SourceName);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new CocoDosReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
