using System.Collections.Generic;

namespace NyaFsNand.Types
{
    /// <summary>One logical eraseblock of a UBI volume and the physical eraseblock it lives in.</summary>
    public class NandLebMapping
    {
        public uint LNum;

        /// <summary>Absolute eraseblock number inside the image (useful to find it in a dump).</summary>
        public int Block;

        public uint DataSize;
        public ulong Sqnum;

        public override string ToString() => $"lnum {LNum} -> block {Block}";
    }

    /// <summary>One volume of a UBI device found inside a NAND/MTD image.</summary>
    public class NandUbiVolume
    {
        public uint Id;
        public string Name;
        public uint Type;
        public uint ReservedPebs;
        public int MappedLebs;

        /// <summary>LEB -> PEB mapping, sorted by logical eraseblock number.</summary>
        public readonly List<NandLebMapping> Lebs = new List<NandLebMapping>();

        public string TypeName => (Type == 1) ? "dynamic" : ((Type == 2) ? "static" : "?");

        public override string ToString() =>
            $"vol {Id} '{Name}' {TypeName}, {ReservedPebs} PEBs, {MappedLebs} mapped LEBs";
    }

    /// <summary>A UBI device (one volume table) found inside a NAND/MTD image.</summary>
    public class NandUbiDevice
    {
        public long Offset;

        /// <summary>Eraseblocks with a valid erase-counter header.</summary>
        public int Pebs;

        /// <summary>Eraseblocks whose erase-counter header exists but fails its CRC.</summary>
        public int BadEcPebs;

        /// <summary>Fully erased eraseblocks (no erase-counter header at all).</summary>
        public int ErasedPebs;

        public int LayoutPebs;
        public long ImageSeq;
        public long MinEc;
        public long MaxEc;
        public double MeanEc;

        public readonly List<NandUbiVolume> Volumes = new List<NandUbiVolume>();

        public NandUbiVolume FindVolume(string NameOrId)
        {
            if (string.IsNullOrEmpty(NameOrId))
                return null;

            foreach (var V in Volumes)
                if (string.Equals(V.Name, NameOrId, System.StringComparison.Ordinal))
                    return V;

            if (uint.TryParse(NameOrId, out var Id))
                foreach (var V in Volumes)
                    if (V.Id == Id)
                        return V;

            return null;
        }

        public override string ToString() =>
            $"UBI @0x{Offset:x}: {Pebs} PEBs, image_seq {ImageSeq}, EC {MinEc}/{MeanEc:0.#}/{MaxEc}";
    }
}
