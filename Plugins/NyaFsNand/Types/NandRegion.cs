namespace NyaFsNand.Types
{
    /// <summary>
    /// A contiguous block-aligned area of the logical (OOB-stripped) NAND image.
    /// <see cref="Offset"/> and <see cref="Size"/> are expressed in logical bytes, so they
    /// can be used both for reporting and for <see cref="NandImage.ReadLogical"/>.
    /// </summary>
    public class NandRegion
    {
        public readonly string Name;
        public readonly string Kind;
        public readonly long Offset;
        public readonly long Size;

        public NandRegion(string Name, string Kind, long Offset, long Size)
        {
            this.Name = Name;
            this.Kind = Kind;
            this.Offset = Offset;
            this.Size = Size;
        }

        public override string ToString() => $"{Name}: {Kind} @0x{Offset:x} ({Size} bytes)";
    }
}
