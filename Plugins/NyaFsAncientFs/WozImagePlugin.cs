using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsAncientFs
{
    /// <summary>
    /// WOZ Apple II 5.25" wrapper. Decodes the track bitstream into a ProDOS
    /// logical-sector image exposed as a `disk.po` child.
    /// </summary>
    public class WozImagePlugin : CompositeImagePlugin
    {
        public WozImagePlugin() : base("image.woz", "woz")
        {
        }

        public override string DisplayName => "WOZ Apple II";

        public override string[] Aliases => new[] { "woz1", "woz2" };

        public override string[] Extensions => new[] { "woz" };

        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension | DetectionCapabilities.ExplicitFormat;

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 12)
                return false;
            return WozImage.TryRead(Image.ReadArray(0, Image.Length), out _);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            if (!WozImage.TryRead(Image.ReadArray(0, Image.Length), out var Woz))
                return Array.Empty<ContainedImage>();

            return new[]
            {
                new ContainedImage("disk.po", ImageKind.Unknown, null, 0, Woz.ProDosImage.Length,
                    Woz.ProDosImage, NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE, null,
                    "WOZ decoded ProDOS-order sector image")
            };
        }

        public override string ReadDescription(ImageData Image)
        {
            if (!WozImage.TryRead(Image.ReadArray(0, Image.Length), out _))
                return null;
            return "WOZ Apple II 5.25\" (ProDOS order)";
        }
    }
}
