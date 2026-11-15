using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsAvalon.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsAvalon
{
    /// <summary>
    /// Canaan Avalon Upgrade Package (AUP). The package is a metadata header
    /// followed by an opaque firmware payload (usually encrypted).
    /// </summary>
    public class NyaFsAvalonCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsAvalonCompositeImagePlugin() : base("image.aup", "aup")
        {
        }

        public override string DisplayName => "Avalon AUP";
        public override string[] Aliases => new[] { "avalon" };
        public override string[] Extensions => new[] { "aup" };
        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension;

        public override bool SupportWrite => true;

        public override bool PreserveUnknownChildren => true;

        public override byte[] Build(BaseImageBlob Blob, out string Error) =>
            new AupWriter().Build(Blob, out Error);

        public override bool DetectImage(ImageData Image)
        {
            if ((Image == null) || (Image.Length < AupImage.CommonHeaderSize))
                return false;
            if (Image.ReadByte(0) != (byte)'A' || Image.ReadByte(1) != (byte)'U' ||
                Image.ReadByte(2) != (byte)'P')
                return false;

            var Version = Image.ReadUInt32(16);
            return Version <= 2;
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Img = new AupImage(Image.ReadArray(0, Image.Length));
            if (!Img.Correct)
                return Array.Empty<ContainedImage>();

            return new[]
            {
                new ContainedImage("payload.bin", ImageKind.Unknown, null,
                    Img.HeaderSize, Img.Payload.Length, Img.Payload,
                    NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE,
                    null, Img.FirmwareVersion)
            };
        }

        public override IEnumerable<KeyValuePair<string, string>> ReadOptions(ImageData Image)
        {
            var Img = new AupImage(Image.ReadArray(0, Image.Length));
            if (!Img.Correct)
                return Array.Empty<KeyValuePair<string, string>>();

            var Res = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("version", Img.Version.ToString()),
                new KeyValuePair<string, string>("firmware", Img.FirmwareVersion),
                new KeyValuePair<string, string>("payload-size", Img.PayloadLength.ToString()),
                new KeyValuePair<string, string>("payload-crc", "0x" + Img.PayloadCrc.ToString("x8"))
            };

            if (Img.HardwareTypes.Length > 0)
                Res.Add(new KeyValuePair<string, string>("hardware", String.Join(",", Img.HardwareTypes)));
            if (Img.SoftwareTypes.Length > 0)
                Res.Add(new KeyValuePair<string, string>("software", String.Join(",", Img.SoftwareTypes)));

            return Res;
        }

        public override string ReadDescription(ImageData Image)
        {
            var Img = new AupImage(Image.ReadArray(0, Image.Length));
            if (!Img.Correct)
                return null;

            var Text = new StringBuilder();
            Text.Append("Avalon AUP v").Append(Img.Version);
            if (Img.FirmwareVersion.Length > 0)
                Text.Append(", firmware ").Append(Img.FirmwareVersion);
            if (Img.HardwareTypes.Length > 0)
                Text.Append(", HW ").Append(String.Join(",", Img.HardwareTypes));
            if (Img.SoftwareTypes.Length > 0)
                Text.Append(", SW ").Append(String.Join(",", Img.SoftwareTypes));
            return Text.ToString();
        }
    }
}
