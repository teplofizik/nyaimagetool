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

                using (var Stream = new System.IO.MemoryStream())
                {
                    if (!BuildFromLayoutTo(Stream, Root, Plugins ?? PluginHost.Current, out Error))
                        return null;

                    return Stream.ToArray();
                }
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        public static bool BuildTo(System.IO.Stream Output, BaseImageBlob Blob,
            PluginRegistry Plugins, out string Error)
        {
            Error = null;

            try
            {
                var Root = Blob?.BuildPlan?.Root;
                if (Root == null)
                {
                    Error = "NAND writer needs a build plan";
                    return false;
                }

                if (Root.Raw != null)
                {
                    var Data = BuildFromSource(Root, out Error);
                    if (Data == null)
                        return false;

                    Output.Write(Data, 0, Data.Length);
                    return true;
                }

                return BuildFromLayoutTo(Output, Root, Plugins ?? PluginHost.Current, out Error);
            }
            catch (Exception E)
            {
                Error = E.Message;
                return false;
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

                if (Child.Removed)
                {
                    // Dropped region: return it to the erased state (0xFF), keep the layout.
                    FillErased(Result, (int)Region.Offset, (int)Region.Size);
                    continue;
                }

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

        private static bool BuildFromLayoutTo(System.IO.Stream Output, ImageBuildNode Root,
            PluginRegistry Plugins, out string Error)
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
                return false;
            }

            var Slots = new List<PlanSlot>();

            Root.Options.TryGetValue("plan.base", out var BaseDir);
            Root.Options.TryGetValue("plan.blobs", out var BlobDir);

            foreach (var Child in Root.Children)
            {
                if (Child.Removed)
                    continue;

                byte[] Data = null;
                long DataLength;
                Func<System.IO.Stream, string> WriteData = null;
                bool Skip = false;

                if (PlanNodeSerializer.TryProbeStreaming(Child, Plugins, BaseDir, BlobDir,
                    out var StreamLength, out var Handled, out var StreamError))
                {
                    DataLength = StreamLength;
                    var Node = Child;
                    WriteData = Stream =>
                    {
                        if (!PlanNodeSerializer.TryWriteStreaming(Node, Plugins, BaseDir, BlobDir, Stream, out _, out var WriteError))
                            return WriteError ?? "Cannot write streamable content";
                        return null;
                    };
                }
                else if (Handled)
                {
                    Error = StreamError;
                    return false;
                }
                else
                {
                    if (!PlanNodeSerializer.TrySerialize(Child, Plugins, BaseDir, BlobDir, out Data, out Skip, out var Err))
                    {
                        Error = Err;
                        return false;
                    }

                    DataLength = Data?.Length ?? 0;
                }

                if (Skip)
                    continue;

                long? Size = Child.Size;
                var Auto = Child.SizeAuto || !Size.HasValue;
                if (Auto)
                    Size = RoundUp(DataLength, Block);

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
                    Data = Data,
                    DataLength = DataLength,
                    WriteData = WriteData
                });
            }

            return PlanPacker.PackTo(Output, Total, Root.Fill, Block, Slots, out Error);
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
