using System;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFsNand.Types;

namespace NyaFsNand
{
    /// <summary>
    /// Writes a logical (no OOB/ECC) flash image by preserving the source layout and
    /// replacing only the changed regions. The whole image is emitted as page data only:
    /// a raw dump turns into a linear image, a linear image round-trips byte for byte.
    ///
    /// Regions keep their source size; a replacement larger than its region is rejected.
    /// Writing a raw (OOB) dump back is intentionally not supported - the vendor ECC would
    /// have to be regenerated.
    /// </summary>
    public static class NandImageWriter
    {
        public static byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;

            try
            {
                var Root = Blob?.BuildPlan?.Root;
                var Source = Root?.Raw;
                if (Source == null)
                {
                    Error = "NAND writer needs an existing source image";
                    return null;
                }

                var Nand = NandImage.TryParse(Source, 0, Source.Length, Root.Name, true);
                if (Nand == null)
                {
                    Error = "Source is not a valid NAND/MTD image";
                    return null;
                }

                var Geometry = Nand.Geometry;
                var TotalBlocks = Source.Length / Geometry.RawBlockSize;
                var Result = Nand.ReadLogical(0, TotalBlocks * Geometry.BlockSize);

                foreach (var Region in Nand.Regions)
                {
                    var Child = FindChild(Root, Region.Name);
                    if (Child == null)
                        continue;

                    var Data = GetChildData(Child);
                    if (Data == null)
                        continue;

                    if (Data.Length > Region.Size)
                    {
                        Error = $"Region '{Region.Name}' is too large for its source slot " +
                            $"({Data.Length} > {Region.Size} bytes)";
                        return null;
                    }

                    Buffer.BlockCopy(Data, 0, Result, (int)Region.Offset, Data.Length);

                    // The rest of a replaced region is erased flash, not zeroes.
                    if (Data.Length < Region.Size)
                        FillErased(Result, (int)(Region.Offset + Data.Length), (int)(Region.Size - Data.Length));
                }

                return Result;
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        private static void FillErased(byte[] Data, int Offset, int Length)
        {
            for (int I = 0; I < Length; I++)
                Data[Offset + I] = 0xFF;
        }

        private static ImageBuildNode FindChild(ImageBuildNode Node, string Name)
        {
            if (Node == null)
                return null;

            foreach (var Child in Node.Children)
            {
                if (String.Equals(Child.Name, Name, StringComparison.OrdinalIgnoreCase))
                    return Child;

                var Res = FindChild(Child, Name);
                if (Res != null)
                    return Res;
            }

            return null;
        }

        private static byte[] GetChildData(ImageBuildNode Child)
        {
            if (!Child.Dirty && (Child.Raw != null))
                return Child.Raw;

            var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Child));
            var Result = Writer.Build(out _);
            return (Result != null) ? Result : Child.Raw;
        }
    }
}
