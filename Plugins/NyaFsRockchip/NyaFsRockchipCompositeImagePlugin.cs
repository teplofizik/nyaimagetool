using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsRockchip.Types;
using System;
using System.Collections.Generic;

namespace NyaFsRockchip
{
    /// <summary>
    /// Rockchip RKFW update image: MiniLoader + RKAF firmware partition table
    /// and partition payloads. Partitions are exposed as Unknown so the normal
    /// pipeline detects Android/ext4/FIT/... inside them.
    /// </summary>
    public class NyaFsRockchipCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsRockchipCompositeImagePlugin() : base("image.rkfw", "rkfw")
        {
        }

        public override bool SupportWrite => true;

        public override byte[] Build(BaseImageBlob Blob, out string Error) =>
            new RkfwWriter().Build(Blob, out Error);

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 0x40)
                return false;
            return Image.ReadUInt32(0) == 0x57464B52U; // RKFW
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Img = new RkfwImage(Image.ReadArray(0, Image.Length));
            if (!Img.Correct)
                return new ContainedImage[0];

            var Res = new List<ContainedImage>();
            var Loader = Img.FirmwareLoader;
            if ((Loader != null) && (Loader.Length > 0))
                Res.Add(new ContainedImage("MiniLoaderAll.bin", ImageKind.Unknown, null, -1, Loader.Length, Loader));

            var Fw = Img.FirmwareData;
            if (Fw == null || !Fw.Correct)
                return Res.ToArray();

            for (var I = 0; I < Fw.FileCount; I++)
            {
                var Header = Fw.GetHeader(I);
                if (Header == null)
                    continue;

                var Name = (Header.Name ?? "").Trim();
                if (Name.Length == 0)
                    Name = (Header.FileName ?? $"partition{I}").Trim();
                if (Name.Length == 0)
                    Name = $"partition{I}";

                var Data = Fw.GetPartition(Header.Name);
                if (Data == null)
                    continue;

                Res.Add(new ContainedImage(Name, ImageKind.Unknown, null, -1, Data.Length, Data));
            }

            return Res.ToArray();
        }
    }
}