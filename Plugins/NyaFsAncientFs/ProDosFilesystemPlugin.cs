using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only ProDOS 8 (.po) filesystem plugin.</summary>
    public class ProDosFilesystemPlugin : FilesystemPlugin
    {
        public ProDosFilesystemPlugin() : base("fs.prodos", "prodos", true, false, false)
        {
        }

        public override string DisplayName => "ProDOS 8";

        public override string[] Aliases => new[] { "po", "prodos8", "apple2" };

        public override string[] Extensions => new[] { "po", "dsk" };

        public override bool DetectFilesystem(ImageData Image) =>
            ProDosReader.IsProDos(Image.Raw, Image.SourceName);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new ProDosReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
