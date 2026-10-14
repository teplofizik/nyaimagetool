using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsAllwinner.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAllwinner
{
    /// <summary>
    /// Allwinner "IMAGEWTY" firmware image: a container of partitions (each with a 1024-byte
    /// file header). See FORMAT.md / awutils imagewty.h. Partitions are returned as Unknown so
    /// the pipeline re-runs detection on each (FIT / uImage / squashfs / ext4 / ...).
    /// </summary>
    public class NyaFsAllwinnerCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsAllwinnerCompositeImagePlugin() : base("image.wty", "wty")
        {
        }

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 0x20)
                return false;

            var Magic = "IMAGEWTY";
            for (int i = 0; i < Magic.Length; i++)
                if (Image.ReadByte(i) != (byte)Magic[i])
                    return false;

            var Version = Image.ReadUInt32(8);
            return (Version == 0x0100) || (Version == 0x0300);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            try
            {
                var Wty = new WtyImage(Image.ReadArray(0, Image.Length));
                if (!Wty.Correct)
                    return new ContainedImage[0];

                var Headers = Wty.FileHeaders;
                var Res = new List<ContainedImage>();
                for (int i = 0; i < Headers.Length; i++)
                {
                    var FH = Headers[i];
                    var Name = (FH.Filename ?? "").TrimEnd('\0');
                    if (Name.Length == 0)
                        Name = $"part_{i}";

                    byte[] Content = null;
                    if (((long)FH.Offset + FH.OriginalLength) <= Image.Length)
                        Content = Wty.GetContent(FH);

                    Res.Add(new ContainedImage(Name, ImageKind.Unknown, null, FH.Offset, FH.OriginalLength, Content));
                }

                return Res.ToArray();
            }
            catch
            {
                return new ContainedImage[0];
            }
        }
    }
}
