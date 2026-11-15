using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsRockchip.Types;
using System;
using System.Collections.Generic;

namespace NyaFsRockchip
{
    /// <summary>
    /// Rockchip RK Nano firmware (RKnanoFW). Its magic is at the end of the
    /// 512-byte header, after the resource/stage tables.
    /// </summary>
    public class RknanoImagePlugin : CompositeImagePlugin
    {
        public RknanoImagePlugin() : base("image.rknano", "rknano")
        {
        }

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 512)
                return false;
            return new RknanoImage(Image.ReadArray(0, Image.Length)).Correct;
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Fw = new RknanoImage(Image.ReadArray(0, Image.Length));
            if (!Fw.Correct)
                return new ContainedImage[0];

            var Result = new List<ContainedImage>();
            foreach (var Blob in Fw.Blobs)
                Result.Add(new ContainedImage(Blob.Name, ImageKind.Unknown, null,
                    -1, Blob.Data.Length, Blob.Data));
            return Result.ToArray();
        }

        public override string ReadDescription(ImageData Image) =>
            new RknanoImage(Image.ReadArray(0, Image.Length)) is var Fw && Fw.Correct
                ? $"RK Nano: {Fw.Vendor} {Fw.Model}"
                : null;
    }
}