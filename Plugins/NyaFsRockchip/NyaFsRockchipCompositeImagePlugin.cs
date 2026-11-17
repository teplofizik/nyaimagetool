using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins;
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

        public override bool SupportLayout => true;

        public override ImageBuildNode CreatePlan(PlanDocument Document, out string Error)
        {
            var Template = PlanTemplate.Load(Document, Format, out Error);
            if (Template == null)
                return null;

            var Image = new RkfwImage(Template);
            if (!Image.Correct)
            {
                Error = "Template is not a valid RKFW image";
                return null;
            }

            var Root = new ImageBuildNode
            {
                Name = "rkfw",
                Path = "",
                Kind = ImageKind.Unknown,
                Format = Format,
                Raw = Template
            };

            Root.Options["plan.layout"] = "1";

            foreach (var Child in Document.Children)
                Root.Children.Add(PlanNodes.FromElement(Child, ""));

            return Root;
        }

        public override byte[] Build(BaseImageBlob Blob, out string Error) =>
            new RkfwWriter().Build(Blob, out Error);

        public override byte[] Build(BaseImageBlob Blob, PluginRegistry Plugins, out string Error)
        {
            var Root = Blob?.BuildPlan?.Root;
            if ((Root == null) || !Root.Options.ContainsKey("plan.layout") || !PlanTemplate.HasProvidedContent(Root))
                return Build(Blob, out Error);

            if (!PlanTemplate.ResolveProvided(Root, Plugins, out Error))
                return null;

            return Build(Blob, out Error);
        }

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
            {
                var Update = Img.FirmwareUpdate;
                if ((Update != null) && (Update.Length >= 512))
                    Res.Add(new ContainedImage("embedded-update.img", ImageKind.Unknown, null,
                        -1, Update.Length, Update));
                return Res.ToArray();
            }

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