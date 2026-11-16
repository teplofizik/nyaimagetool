namespace NyaFsNand.Types
{
    /// <summary>
    /// Geometry of a raw NAND dump: the page data area, the spare (OOB) area and the
    /// eraseblock size. Raw dumps interleave page data with the spare area, so every
    /// access has to go through <see cref="RawStride"/>.
    /// </summary>
    public class NandGeometry
    {
        public readonly int PageSize;
        public readonly int OobSize;
        public readonly int PagesPerBlock;

        public NandGeometry(int PageSize, int OobSize, int PagesPerBlock)
        {
            this.PageSize = PageSize;
            this.OobSize = OobSize;
            this.PagesPerBlock = PagesPerBlock;
        }

        /// <summary>Bytes of page data + spare area per physical page.</summary>
        public int RawStride => PageSize + OobSize;

        /// <summary>Logical (data-only) size of one eraseblock.</summary>
        public int BlockSize => PageSize * PagesPerBlock;

        /// <summary>Physical size of one eraseblock, including the spare area.</summary>
        public int RawBlockSize => RawStride * PagesPerBlock;

        public override string ToString() =>
            $"page {PageSize}, oob {OobSize}, block {BlockSize / 1024} KiB ({PagesPerBlock} pages)";
    }
}
