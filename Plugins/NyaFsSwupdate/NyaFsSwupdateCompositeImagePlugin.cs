using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.Filesystem.Cpio.Types;
using NyaFsSwupdate.Types;
using System;
using System.Collections.Generic;

namespace NyaFsSwupdate
{
    /// <summary>
    /// SWUpdate (.swu) package: a CPIO newc archive containing sw-description,
    /// optional signature, images and files. Used by SWUpdate and Canaan
    /// Avalon Nano3 firmware (among others).
    /// </summary>
    public class NyaFsSwupdateCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsSwupdateCompositeImagePlugin() : base("image.swu", "swu")
        {
        }

        public override FilesystemOption[] Options => new[]
        {
            new FilesystemOption("sign-key", FilesystemOptionType.String, null,
                "KeyStore profile used to re-sign sw-description")
        };

        public override bool SupportWrite => true;

        public override bool SupportLayout => true;

        public override ImageBuildNode CreatePlan(PlanDocument Document, out string Error)
        {
            var Template = PlanTemplate.Load(Document, Format, out Error);
            if (Template == null)
                return null;

            var Archive = new SwuArchive(Template);
            if (!Archive.Correct)
            {
                Error = "Template is not a valid SWU CPIO archive";
                return null;
            }

            var Root = new ImageBuildNode
            {
                Name = "swu",
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
            new SwuWriter().Build(Blob, out Error);

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
            if (Image.Length < 6)
                return false;
            if (Image.ReadByte(0) != (byte)'0' || Image.ReadByte(5) != (byte)'2')
                return false;

            var Fw = new SwuArchive(Image.ReadArray(0, Image.Length));
            return Fw.Correct && (Fw.Read("sw-description") != null) && (Fw.Items.Count > 0);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Fw = new SwuArchive(Image.ReadArray(0, Image.Length));
            if (!Fw.Correct)
                return new ContainedImage[0];

            var Res = new List<ContainedImage>();
            foreach (var Node in Fw.Items)
            {
                var Name = (Node.Path ?? "").TrimStart('/');
                if (Name.Length == 0)
                    continue;

                var Content = Node.Content;
                if (Content == null)
                    continue;

                Res.Add(new ContainedImage(Name, ImageKind.Unknown, null, -1, Content.Length, Content));
            }
            return Res.ToArray();
        }
    }
}