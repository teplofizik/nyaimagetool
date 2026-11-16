using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;

namespace NyaFsUboot
{
    /// <summary>
    /// U-Boot environment as a first-class image container: `load env.bin` detects
    /// the CRC32-protected environment, `store` writes it back, and the `env`
    /// commands edit the same loaded plan.
    /// </summary>
    public class UbootEnvImagePlugin : CompositeImagePlugin
    {
        public UbootEnvImagePlugin() : base("image.env", "env") { }

        public override bool SupportWrite => true;

        public override bool PreserveUnknownChildren => true;

        public override bool DetectImage(ImageData Image)
        {
            // Real U-Boot environment sectors are small (usually 64K/128K/256K).
            // Avoid CRC scans over large firmware images during auto-detection.
            if ((Image.Length < 0x40) || (Image.Length > 0x1000000))
                return false;

            return UbootEnvironment.TryParse(Image.ReadArray(0, Image.Length), out _, out _);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            // The environment is a single opaque element. The payload is intentionally
            // not returned as content: re-feeding the same bytes would re-detect the
            // container recursively. The whole source is kept in the build plan root.
            return new ContainedImage[]
            {
                new ContainedImage("environment", ImageKind.Unknown, "env", 0, Image.Length, null)
            };
        }

        public override IEnumerable<KeyValuePair<string, string>> ReadOptions(ImageData Image)
        {
            if (!UbootEnvironment.TryParse(Image.ReadArray(0, Image.Length), out var Env, out _))
                yield break;

            yield return new KeyValuePair<string, string>("entries", Env.Count.ToString());
            yield return new KeyValuePair<string, string>("size", Env.RegionSize.ToString());
            yield return new KeyValuePair<string, string>("copies", Env.Copies.ToString());
            yield return new KeyValuePair<string, string>("data-offset", Env.DataOffset.ToString());
        }

        public override string ReadDescription(ImageData Image) =>
            UbootEnvironment.TryParse(Image.ReadArray(0, Image.Length), out var Env, out _)
                ? $"U-Boot environment ({Env.Count} entries, {Env.RegionSize} bytes)"
                : null;

        public override byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            var Root = Blob.BuildPlan?.Root;
            if (Root?.Raw == null)
            {
                Error = "U-Boot environment writer needs a loaded environment";
                return null;
            }

            return Root.Raw;
        }
    }
}
