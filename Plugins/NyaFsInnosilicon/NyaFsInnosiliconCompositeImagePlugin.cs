using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.Filesystem.Cpio.Types;
using NyaFsInnosilicon.Types;
using System;
using System.Collections.Generic;

namespace NyaFsInnosilicon
{
    /// <summary>
    /// Innosilicon SWU firmware: a CPIO newc archive containing sw-description,
    /// U-Boot/uImage, devicetree.dtb, rootfs.ubi and vendor blobs.
    /// </summary>
    public class NyaFsInnosiliconCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsInnosiliconCompositeImagePlugin() : base("image.swu", "swu")
        {
        }

        public override FilesystemOption[] Options => new[]
        {
            new FilesystemOption("sign-key", FilesystemOptionType.String, null,
                "KeyStore profile used to re-sign sw-description")
        };

        public override bool SupportWrite => true;

        public override byte[] Build(BaseImageBlob Blob, out string Error) =>
            new SwuWriter().Build(Blob, out Error);

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 6)
                return false;
            if (Image.ReadByte(0) != (byte)'0' || Image.ReadByte(5) != (byte)'2')
                return false;

            var Fw = new InnoFirmwareUpdate(Image.ReadArray(0, Image.Length));
            return Fw.Correct && (Fw.Read("sw-description") != null) && (Fw.Read("rootfs.ubi") != null);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Fw = new InnoFirmwareUpdate(Image.ReadArray(0, Image.Length));
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