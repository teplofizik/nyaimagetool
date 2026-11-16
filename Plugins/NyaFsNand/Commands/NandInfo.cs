using System;
using System.IO;
using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsNand.Types;

namespace NyaFsNand.Commands
{
    /// <summary>
    /// nandinfo {file} [--blocks] [volume] - geometry, bad blocks, regions and UBI volumes
    /// of a raw NAND dump or a logical (no OOB) MTD image.
    ///
    /// --blocks prints the per-eraseblock state; a volume name (or id) prints the
    /// LEB -> PEB mapping of that volume.
    /// </summary>
    class NandInfo : ScriptStepGenerator
    {
        private const string BlocksFlag = "--blocks";

        public NandInfo() : base("nandinfo")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam()
            }));

            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("option")
            }));

            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("option"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("volume")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;
            string Volume = null;
            var ShowBlocks = false;

            for (int I = 1; I < A.Length; I++)
            {
                if (String.Equals(A[I], BlocksFlag, StringComparison.OrdinalIgnoreCase))
                    ShowBlocks = true;
                else
                    Volume = A[I];
            }

            return new NandInfoStep(A[0], Volume, ShowBlocks);
        }

        public class NandInfoStep : ScriptStep
        {
            private readonly string Path;
            private readonly string Volume;
            private readonly bool ShowBlocks;

            public NandInfoStep(string Path, string Volume, bool ShowBlocks) : base("nandinfo")
            {
                this.Path = Path;
                this.Volume = Volume;
                this.ShowBlocks = ShowBlocks;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                if (!File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = NandImage.TryParse(Data, 0, Data.Length, Path, true);
                if (Image == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not a NAND/MTD image!");

                var G = Image.Geometry;
                var Blocks = Image.Length / G.RawBlockSize;

                Log.Write(0, $"NAND image: {(Image.Logical ? "logical MTD image (no OOB/ECC)" : "raw chip dump (with OOB/ECC)")}");
                Log.Write(0, $"  geometry: page {G.PageSize}, oob {G.OobSize}, block {G.BlockSize / 1024} KiB ({G.PagesPerBlock} pages), {Blocks} blocks");

                var Bad = Image.BadBlocks;
                if (Image.Logical)
                    Log.Write(0, "  bad blocks: n/a (no spare area)");
                else if (Bad < 0)
                    Log.Write(0, "  bad blocks: unknown (spare bytes 0..1 are not used as markers)");
                else
                    Log.Write(0, $"  bad blocks: {Bad}");

                Log.Write(0, $"  regions: {Image.Regions.Length}");

                foreach (var R in Image.Regions)
                    Log.Write(0, $"    {R.Name}: {R.Kind}, {R.Size} bytes ({R.Size / G.BlockSize} blocks) @0x{R.Offset:x}");

                var Devices = Image.ReadUbiDevices();
                if (Devices.Count == 0)
                    Log.Write(0, "  UBI: not found");
                else
                {
                    foreach (var D in Devices)
                    {
                        var Total = D.Pebs + D.BadEcPebs + D.ErasedPebs;

                        Log.Write(0, $"  UBI device @0x{D.Offset:x}: {D.Pebs} PEBs, {D.LayoutPebs} layout PEBs, image_seq {D.ImageSeq}");
                        Log.Write(0, $"    eraseblocks: {D.Pebs} good, {D.BadEcPebs} bad EC, {D.ErasedPebs} erased (of {Total})");
                        Log.Write(0, $"    erase counters: min {D.MinEc}, mean {D.MeanEc:0.#}, max {D.MaxEc}");

                        foreach (var V in D.Volumes)
                            Log.Write(0, $"    vol {V.Id} '{V.Name ?? "?"}' {V.TypeName}, reserved {V.ReservedPebs} PEBs, mapped {V.MappedLebs} LEBs");
                    }
                }

                if (ShowBlocks)
                    PrintBlocks(Image);

                if (Volume == null)
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);

                foreach (var D in Devices)
                {
                    var V = D.FindVolume(Volume);
                    if (V == null)
                        continue;

                    Log.Write(0, $"  LEB -> PEB map for vol {V.Id} '{V.Name ?? "?"}' ({V.Lebs.Count} mapped LEBs):");

                    foreach (var L in V.Lebs)
                        Log.Write(0, $"    lnum {L.LNum,5} -> block {L.Block,5} @0x{L.Block * (long)Image.Geometry.RawBlockSize:x}" +
                            ((L.DataSize > 0) ? $", data {L.DataSize} bytes" : ""));

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }

                return new ScriptStepResult(ScriptStepStatus.Error, $"Volume '{Volume}' is not found!");
            }

            private static void PrintBlocks(NandImage Image)
            {
                var Table = Image.ReadBlocks();

                Log.Write(0, $"  block table ({Table.Count} blocks):");

                foreach (var B in Table)
                {
                    string State;
                    string Detail;

                    if (B.Ubi)
                    {
                        State = "ubi";

                        if (!B.EcOk)
                            Detail = "bad EC";
                        else if (B.Layout)
                            Detail = $"layout lnum {B.LNum}, ec {B.Ec}";
                        else if (B.HasVid)
                            Detail = $"vol {B.VolumeId} lnum {B.LNum}, ec {B.Ec}";
                        else
                            Detail = $"free (no VID), ec {B.Ec}";
                    }
                    else if (B.Used)
                    {
                        State = "data";
                        Detail = B.Container ?? "-";
                    }
                    else
                    {
                        State = "erased";
                        Detail = "";
                    }

                    Log.Write(0, $"    {B.Index,5} @0x{B.RawOffset:x8} {State,-6} {Detail}");
                }
            }
        }
    }
}
