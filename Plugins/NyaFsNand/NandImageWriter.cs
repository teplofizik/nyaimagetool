using System;
using System.Collections.Generic;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Elements.Fs.Writer;
using NyaFs.ImageFormat.Plugins;
using NyaFsNand.Types;

namespace NyaFsNand
{
    /// <summary>
    /// Writes a logical (no OOB/ECC) flash image.
    ///
    /// Two modes:
    /// - source mode: the plan root has Raw bytes (a loaded image); the source layout is
    ///   preserved and only changed regions are replaced;
    /// - layout mode: the root has no Raw (created by `mkimage`); the image is built from
    ///   the declarative slots and the content of the plan nodes.
    ///
    /// Regions keep their source size; a replacement larger than its region is rejected.
    /// Writing a raw (OOB) dump back is intentionally not supported.
    /// </summary>
    public static class NandImageWriter
    {
        public static byte[] Build(BaseImageBlob Blob, out string Error) =>
            Build(Blob, PluginHost.Current, out Error);

        public static byte[] Build(BaseImageBlob Blob, PluginRegistry Plugins, out string Error)
        {
            Error = null;

            try
            {
                var Root = Blob?.BuildPlan?.Root;
                if (Root == null)
                {
                    Error = "NAND writer needs a build plan";
                    return null;
                }

                if (Root.Raw != null)
                    return BuildFromSource(Root, out Error);

                return BuildFromLayout(Root, Plugins ?? PluginHost.Current, out Error);
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        // ---- source mode (template + replacements) ----

        private static byte[] BuildFromSource(ImageBuildNode Root, out string Error)
        {
            Error = null;

            var Source = Root.Raw;
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

                if (Data.Length < Region.Size)
                    FillErased(Result, (int)(Region.Offset + Data.Length), (int)(Region.Size - Data.Length));
            }

            return Result;
        }

        // ---- layout mode (build from scratch) ----

        private static byte[] BuildFromLayout(ImageBuildNode Root, PluginRegistry Plugins, out string Error)
        {
            Error = null;

            var Page = Option(Root, "page", 2048);
            var Block = Option(Root, "block", 0x20000);
            var Total = Option(Root, "total", 0);

            if (Total <= 0)
            {
                var Blocks = Option(Root, "blocks", 0);
                Total = Blocks * Block;
            }

            if ((Total <= 0) || (Block <= 0) || ((Total % Block) != 0))
            {
                Error = "NAND layout needs a positive 'total' (multiple of 'block') or 'blocks'";
                return null;
            }

            var Slots = new List<PlanSlot>();

            Root.Options.TryGetValue("plan.base", out var BaseDir);
            Root.Options.TryGetValue("plan.blobs", out var BlobDir);

            foreach (var Child in Root.Children)
            {
                if (!TrySerializeNode(Child, Plugins, BaseDir, BlobDir, out var Data, out var Skip, out var Err))
                {
                    Error = Err;
                    return null;
                }

                if (Skip)
                    continue;

                long? Size = Child.Size;
                var Auto = Child.SizeAuto || !Size.HasValue;
                if (Auto)
                    Size = RoundUp(Data.Length, Block);

                Slots.Add(new PlanSlot
                {
                    Name = Child.Name,
                    Offset = Child.Offset,
                    Size = Size,
                    SizeAuto = Auto,
                    Align = Child.Align ?? Block,
                    RepeatCount = Child.RepeatCount,
                    RepeatStride = Child.RepeatStride,
                    Fill = Child.Fill,
                    Data = Data
                });
            }

            return PlanPacker.Pack(Total, Root.Fill, Block, Slots, out Error);
        }

        private static bool TrySerializeNode(ImageBuildNode Node, PluginRegistry Plugins,
            string BaseDir, string BlobDir, out byte[] Data, out bool Skip, out string Error)
        {
            Data = null;
            Skip = false;
            Error = null;

            if (PlanContentResolver.TryResolve(Node, Plugins, BaseDir, BlobDir, out var ContentData, out var ContentSkip, out var ContentError))
            {
                if (!String.IsNullOrEmpty(ContentError))
                {
                    Error = ContentError;
                    return false;
                }

                if (ContentSkip)
                {
                    Skip = true;
                    return true;
                }

                Data = ContentData;
                return true;
            }

            if (Node.Fs != null)
            {
                var Format = !String.IsNullOrEmpty(Node.Format) ? Node.Format : Node.Fs.FilesystemFormat;
                var Plugin = Plugins?.ResolveFilesystem(Format) ?? Plugins?.ResolveFilesystem(Node.Fs.FilesystemFormat);
                var Writer = Plugin?.GetFilesystemWriter(Node.Fs);

                if (Writer == null)
                {
                    Error = $"No writer for filesystem '{Format}' at '{Node.Path}'";
                    return false;
                }

                if (!Writer.CheckFilesystem(Node.Fs))
                {
                    Error = $"Filesystem '{Format}' at '{Node.Path}' cannot be serialized";
                    return false;
                }

                Writer.WriteFs(Node.Fs);
                if (!Writer.HasRawStreamData)
                {
                    Error = $"Filesystem '{Format}' at '{Node.Path}' produced no data";
                    return false;
                }

                Data = Writer.RawStream;
                return true;
            }

            if (Node.Children.Count > 0)
            {
                if (!PlanMaterializer.Apply(Node, Plugins, BaseDir, BlobDir, out var MaterializeError))
                {
                    Error = MaterializeError;
                    return false;
                }

                if ((Node.Format == "fit") || (Node.Format == "cimg"))
                {
                    var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Node)) { Registry = Plugins };
                    Data = Writer.Build(out Error);
                    return Data != null;
                }

                if (Node.Format == "android")
                {
                    var Temp = new BaseImageBlob { BuildPlan = new ImageBuildPlan(Node) };

                    foreach (var Child in Node.Children)
                    {
                        if (Child.Kernel != null)
                            Temp.SetKernel(0, Child.Kernel);
                        if (Child.Fs != null)
                            Temp.SetFilesystem(0, Child.Fs);
                        if (Child.Dtb != null)
                            Temp.SetDevTree(0, Child.Dtb);
                    }

                    Data = new AndroidImageWriter("") { Registry = Plugins }.Build(Temp, out Error);
                    return Data != null;
                }

                Error = $"Container '{Node.Format}' at '{Node.Path}' is not supported by the layout writer yet";
                return false;
            }

            if (IsContainerFormat(Node.Format))
            {
                Error = $"Container '{Node.Format}' at '{Node.Path}' has no children; add its content or a nested layout";
                return false;
            }

            if ((Node.Raw != null) && (Node.Raw.Length > 0))
            {
                Data = Node.Raw;
                return true;
            }

            if (Node.Raw != null)
            {
                // Untouched slot: leave it erased.
                Skip = true;
                return true;
            }

            Error = $"Slot '{Node.Path}' has no content";
            return false;
        }

        private static bool IsContainerFormat(string Format)
        {
            if (String.IsNullOrEmpty(Format))
                return false;

            switch (Format.ToLowerInvariant())
            {
                case "fit":
                case "android":
                case "cimg":
                case "ubi":
                case "nand":
                case "disk":
                case "swu":
                case "rkfw":
                    return true;
                default:
                    return false;
            }
        }

        private static long Option(ImageBuildNode Node, string Key, long Default)
        {
            if (!Node.Options.TryGetValue(Key, out var Text) || String.IsNullOrWhiteSpace(Text))
                return Default;

            var Value = PlanDocument.ParseSize(Text, out var Auto);
            return (Auto || !Value.HasValue) ? Default : Value.Value;
        }

        private static long RoundUp(long Value, long Align)
        {
            if (Align <= 1)
                return Value;
            return ((Value + Align - 1) / Align) * Align;
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
