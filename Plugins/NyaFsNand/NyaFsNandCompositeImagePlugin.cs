using System;
using System.Collections.Generic;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFsNand.Types;

namespace NyaFsNand
{
    /// <summary>
    /// Raw NAND chip dump (page data interleaved with the spare/OOB area).
    ///
    /// There is no container magic, so the geometry (page size, spare size, eraseblock
    /// size) is recovered heuristically from the dump itself and used areas are exposed
    /// as block-aligned "Unknown" children with the OOB stripped. The regular pipeline
    /// then detects UBI/UBIFS/Android/ext/squashfs/... inside every region.
    ///
    /// Alias "mtd"/"nanddump" or explicit format "nand" can be used to force loading:
    ///   load dump.bin all nand
    /// </summary>
    public class NyaFsNandCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsNandCompositeImagePlugin() : base("image.nand", "nand")
        {
        }

        public override string DisplayName => "Raw NAND dump";
        public override string[] Aliases => new[] { "mtd", "nanddump", "rawnand" };
        public override string[] Extensions => new[] { ".nand", ".dump", ".mtd" };

        /// <summary>
        /// Raw dumps have no magic. "Magic" here means the heuristic geometry probe; the
        /// specialised extensions are hints that allow a looser probe.
        /// </summary>
        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension;

        public override bool SupportWrite => false;

        /// <summary>Keep even fully encrypted regions so they can still be listed/extracted.</summary>
        public override bool PreserveUnknownChildren => true;

        private readonly object CacheLock = new object();
        private byte[] CacheRaw;
        private long CacheBase;
        private long CacheLength;
        private NandImage CacheImage;

        private NandImage Parse(ImageData Image, bool AllowWithoutSpare)
        {
            if ((Image == null) || (Image.Raw == null))
                return null;

            lock (CacheLock)
            {
                if (ReferenceEquals(CacheRaw, Image.Raw) &&
                    (CacheBase == Image.Offset) && (CacheLength == Image.Length))
                    return CacheImage;

                var Parsed = NandImage.TryParse(Image.Raw, Image.Offset, Image.Length, Image.SourceName, AllowWithoutSpare);

                CacheRaw = Image.Raw;
                CacheBase = Image.Offset;
                CacheLength = Image.Length;
                CacheImage = Parsed;

                return Parsed;
            }
        }

        public override bool DetectImage(ImageData Image) =>
            (Image != null) &&
            (NandImage.DetectGeometry(Image.Raw, Image.Offset, Image.Length, Image.SourceName) != null);

        public override string ReadDescription(ImageData Image)
        {
            var Nand = Parse(Image, false);
            if (Nand == null)
                return null;

            return $"NAND dump: {Nand.Geometry}, {Nand.Regions.Length} used region(s)";
        }

        public override IEnumerable<KeyValuePair<string, string>> ReadOptions(ImageData Image)
        {
            var Nand = Parse(Image, false);
            if (Nand == null)
                return Array.Empty<KeyValuePair<string, string>>();

            return new[]
            {
                new KeyValuePair<string, string>("page", Nand.Geometry.PageSize.ToString()),
                new KeyValuePair<string, string>("oob", Nand.Geometry.OobSize.ToString()),
                new KeyValuePair<string, string>("block", Nand.Geometry.BlockSize.ToString()),
                new KeyValuePair<string, string>("blocks", (Nand.Length / Nand.Geometry.RawBlockSize).ToString()),
                new KeyValuePair<string, string>("regions", Nand.Regions.Length.ToString())
            };
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Nand = Parse(Image, true);
            if (Nand == null)
                return Array.Empty<ContainedImage>();

            var Result = new List<ContainedImage>();
            foreach (var Region in Nand.Regions)
            {
                if (Region.Size <= 0)
                    continue;

                var Content = Nand.ReadLogical(Region.Offset, Region.Size);
                Result.Add(new ContainedImage(Region.Name, ImageKind.Unknown, null,
                    Region.Offset, Region.Size, Content));
            }

            return Result.ToArray();
        }
    }
}
