using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;

namespace NyaFsAncientFs
{
    /// <summary>Read-only Commodore DOS (.d64) filesystem plugin.</summary>
    public class CbmDosFilesystemPlugin : FilesystemPlugin
    {
        public CbmDosFilesystemPlugin() : base("fs.cbm", "cbm", true, false, false)
        {
        }

        public override string DisplayName => "Commodore DOS (1541)";

        public override string[] Aliases => new[] { "d64", "c64", "1541", "commodore" };

        public override string[] Extensions => new[] { "d64" };

        public override bool DetectFilesystem(ImageData Image) =>
            CbmDosReader.IsCbmDos(Image.Raw, Image.SourceName);

        public override NyaFs.ImageFormat.Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data)
        {
            var Reader = new CbmDosReader(Data);
            return new BaseFsReader(NyaFs.ImageFormat.Types.FsType.Unknown, Reader, Reader.VolumeBytes);
        }
    }
}
