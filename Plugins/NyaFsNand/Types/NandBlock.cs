namespace NyaFsNand.Types
{
    /// <summary>State of one eraseblock of a NAND/MTD image.</summary>
    public class NandBlock
    {
        /// <summary>Eraseblock number inside the image.</summary>
        public int Index;

        /// <summary>Absolute offset of the eraseblock in the image (with OOB when present).</summary>
        public long RawOffset;

        /// <summary>Logical offset of the eraseblock (without OOB).</summary>
        public long LogicalOffset;

        /// <summary>Any page in the block contains data (not all 0xFF).</summary>
        public bool Used;

        /// <summary>Block starts with a UBI erase-counter header.</summary>
        public bool Ubi;

        /// <summary>The UBI erase-counter header CRC is valid.</summary>
        public bool EcOk;

        /// <summary>Valid volume identifier header is present at the declared offset.</summary>
        public bool HasVid;

        /// <summary>This is a UBI layout-volume PEB (holds the volume table).</summary>
        public bool Layout;

        public uint VolumeId;
        public uint LNum;
        public long Ec;

        /// <summary>Recognised container at the block start for non-UBI data blocks.</summary>
        public string Container;
    }
}
