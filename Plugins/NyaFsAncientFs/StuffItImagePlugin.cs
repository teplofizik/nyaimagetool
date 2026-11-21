using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsAncientFs
{
    /// <summary>
    /// StuffIt 1.x/3.x archive wrapper. Data forks are exposed as child files;
    /// Disk Copy/MFS/HFS detection then runs on the extracted content.
    /// </summary>
    public class StuffItImagePlugin : CompositeImagePlugin
    {
        public StuffItImagePlugin() : base("image.sit", "sit")
        {
        }

        public override string DisplayName => "StuffIt";

        public override string[] Aliases => new[] { "stuffit", "sit1", "sit3" };

        public override string[] Extensions => new[] { "sit" };

        public override DetectionCapabilities DetectCapabilities =>
            DetectionCapabilities.Magic | DetectionCapabilities.Extension | DetectionCapabilities.ExplicitFormat;

        public override bool DetectImage(ImageData Image) =>
            StuffItArchive.IsStuffIt(Image.Raw);

        public override ContainedImage[] GetImages(ImageData Image)
        {
            if (!DetectImage(Image))
                return Array.Empty<ContainedImage>();

            try
            {
                var Items = new StuffItArchive(Image.ReadArray(0, Image.Length)).ReadItems();
                var Result = new ContainedImage[Items.Count];
                for (int I = 0; I < Items.Count; I++)
                {
                    Result[I] = new ContainedImage(
                        Items[I].Name,
                        ImageKind.Unknown,
                        null,
                        -1,
                        Items[I].Data.Length,
                        Items[I].Data);
                }
                return Result;
            }
            catch
            {
                return Array.Empty<ContainedImage>();
            }
        }

        public override string ReadDescription(ImageData Image)
        {
            if (!DetectImage(Image))
                return null;
            return "StuffIt archive";
        }
    }
}
