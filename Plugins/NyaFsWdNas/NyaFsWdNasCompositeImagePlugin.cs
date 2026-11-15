using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsWdNas.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsWdNas
{
    /// <summary>
    /// Western Digital My Cloud DL2100 firmware .bin container.
    /// Reference: https://github.com/michaelroland/wdnas-firmware-modding
    /// </summary>
    public class NyaFsWdNasCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsWdNasCompositeImagePlugin() : base("image.wdnas", "wdnas")
        {
        }

        public override string DisplayName => "WD NAS firmware";
        public override string[] Aliases => new[] { "wd", "dl2100" };
        public override string[] Extensions => new[] { "bin" };
        public override DetectionCapabilities DetectCapabilities => DetectionCapabilities.Magic;

        public override bool SupportWrite => true;

        public override byte[] Build(BaseImageBlob Blob, out string Error) =>
            new WdNasWriter().Build(Blob, out Error);

        public override bool DetectImage(ImageData Image)
        {
            if ((Image == null) || (Image.Length < WdNasImage.MainHeaderSize))
                return false;
            return Image.ReadByte(48) == 0x55 && Image.ReadByte(49) == 0xAA &&
                Image.ReadByte(58) == 0x55 && Image.ReadByte(59) == 0xAA;
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Fw = new WdNasImage(Image.ReadArray(0, Image.Length));
            if (!Fw.Correct)
                return Array.Empty<ContainedImage>();

            var Result = new List<ContainedImage>();
            foreach (var Item in Fw.Images)
                Result.Add(new ContainedImage(Item.Name, ImageKind.Unknown, null,
                    Item.Offset, Item.Length, Item.Data));

            var SquashFs = Fw.CfsSquashFs;
            if (SquashFs != null)
                Result.Add(new ContainedImage("image.squashfs", ImageKind.Unknown, null,
                    Fw.Image("image.cfs").Length >= WdNasImage.CfsHeaderSize
                        ? Fw.Images[2].Offset + WdNasImage.CfsHeaderSize : -1,
                    SquashFs.Length, SquashFs));

            for (var I = 0; I < Fw.Targets.Length; I++)
            {
                var Target = Fw.Targets[I];
                var Name = String.IsNullOrEmpty(Target.FileName) ? $"ext_{I:0000}" : Target.FileName;
                Result.Add(new ContainedImage(Name, ImageKind.Unknown, null,
                    Target.Offset, Target.Length, Target.Data));
            }
            return Result.ToArray();
        }

        public override IEnumerable<KeyValuePair<string, string>> ReadOptions(ImageData Image)
        {
            var Fw = new WdNasImage(Image.ReadArray(0, Image.Length));
            if (!Fw.Correct)
                return Array.Empty<KeyValuePair<string, string>>();

            var Result = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("codename", Fw.Codename),
                new KeyValuePair<string, string>("version", Fw.FirmwareVersion),
                new KeyValuePair<string, string>("images", Fw.Images.Length.ToString()),
                new KeyValuePair<string, string>("extraction-targets", Fw.Targets.Length.ToString())
            };
            return Result;
        }

        public override string ReadDescription(ImageData Image)
        {
            var Fw = new WdNasImage(Image.ReadArray(0, Image.Length));
            if (!Fw.Correct)
                return null;
            return $"WD NAS {Fw.Codename}, firmware {Fw.FirmwareVersion}";
        }
    }
}
