using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Disk Copy 4.2 image wrapper: 84-byte header, raw volume data, optional tag
    /// area. The raw volume is exposed as a child so MFS/HFS detection can run.
    /// </summary>
    public class DiskCopy42ImagePlugin : CompositeImagePlugin
    {
        private const int HeaderSize = 84;

        public DiskCopy42ImagePlugin() : base("image.dc42", "dc42")
        {
        }

        public override string DisplayName => "Disk Copy 4.2";

        public override string[] Aliases => new[] { "diskcopy", "dc4.2" };

        public override string[] Extensions => new[] { "image" };

        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension | DetectionCapabilities.ExplicitFormat;

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < HeaderSize)
                return false;

            int NameLength = Image.ReadByte(0);
            if (NameLength > 63)
                return false;

            uint DataSize = Image.ReadUInt32BE(0x40);
            uint TagSize = Image.ReadUInt32BE(0x44);
            if ((DataSize < 409600) || (DataSize > 32 * 1024 * 1024) || ((DataSize % 512) != 0))
                return false;
            if ((long)DataSize + TagSize + HeaderSize != Image.Length)
                return false;

            return true;
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            if (!DetectImage(Image))
                return Array.Empty<ContainedImage>();

            uint DataSize = Image.ReadUInt32BE(0x40);
            int NameLength = Image.ReadByte(0);
            string Name = "volume";
            if (NameLength > 0)
            {
                var Chars = new char[NameLength];
                for (int I = 0; I < NameLength; I++)
                    Chars[I] = (char)Image.ReadByte(1 + I);
                Name = new string(Chars);
            }

            var Raw = Image.ReadArray(HeaderSize, DataSize);
            return new[]
            {
                new ContainedImage(Name, ImageKind.Unknown, null, HeaderSize, DataSize, Raw,
                    NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE, null, "Disk Copy 4.2 raw volume")
            };
        }

        public override string ReadDescription(ImageData Image)
        {
            if (!DetectImage(Image))
                return null;

            uint DataSize = Image.ReadUInt32BE(0x40);
            return "Disk Copy 4.2 image (" + (DataSize / 1024) + " KiB)";
        }
    }
}
