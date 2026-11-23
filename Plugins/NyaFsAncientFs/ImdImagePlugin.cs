using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsAncientFs
{
    /// <summary>
    /// ImageDisk wrapper. Uniform-geometry .imd images are exposed as a raw
    /// sector image so the normal filesystem detectors can inspect them.
    /// </summary>
    public class ImdImagePlugin : CompositeImagePlugin
    {
        public ImdImagePlugin() : base("image.imd", "imd")
        {
        }

        public override string DisplayName => "ImageDisk";

        public override string[] Aliases => new[] { "imagedisk" };

        public override string[] Extensions => new[] { "imd" };

        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension | DetectionCapabilities.ExplicitFormat;

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 5)
                return false;
            return ImdImage.TryRead(Image.ReadArray(0, Image.Length), out _);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Data = Image.ReadArray(0, Image.Length);
            if (!ImdImage.TryRead(Data, out var Imd))
                return Array.Empty<ContainedImage>();

            return new[]
            {
                new ContainedImage("disk.dsk", ImageKind.Unknown, null, 0, Imd.Raw.Length,
                    Imd.Raw, NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE, null,
                    "ImageDisk raw sector image")
            };
        }

        public override string ReadDescription(ImageData Image)
        {
            var Data = Image.ReadArray(0, Image.Length);
            if (!ImdImage.TryRead(Data, out var Imd))
                return null;
            return "ImageDisk " + Imd.Tracks + "T/" + Imd.Sides + "S/" +
                Imd.SectorsPerTrack + "S/" + Imd.SectorSize + "B";
        }
    }
}
