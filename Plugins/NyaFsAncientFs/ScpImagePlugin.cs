using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsAncientFs
{
    /// <summary>SCP Apple II flux image wrapper, exposed as a ProDOS-order image.</summary>
    public class ScpImagePlugin : CompositeImagePlugin
    {
        private const int HeaderSize = 16;

        public ScpImagePlugin() : base("image.scp", "scp")
        {
        }

        public override string DisplayName => "SCP Apple II flux";

        public override string[] Extensions => new[] { "scp" };

        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension | DetectionCapabilities.ExplicitFormat;

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < HeaderSize)
                return false;
            return ScpImage.TryRead(Image.ReadArray(0, Image.Length), out _);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            if (!ScpImage.TryRead(Image.ReadArray(0, Image.Length), out var Scp))
                return Array.Empty<ContainedImage>();

            return new[]
            {
                new ContainedImage("disk.po", ImageKind.Unknown, null, 0, Scp.ProDosImage.Length,
                    Scp.ProDosImage, NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE, null,
                    "SCP decoded ProDOS-order sector image")
            };
        }

        public override string ReadDescription(ImageData Image)
        {
            if (!ScpImage.TryRead(Image.ReadArray(0, Image.Length), out _))
                return null;
            return "SCP Apple II flux";
        }
    }
}
