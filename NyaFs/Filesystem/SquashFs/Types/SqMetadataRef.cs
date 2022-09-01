using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    internal class SqMetadataRef
    {
        internal long Value { get; }

        internal SqMetadataRef(long Value)
        {
            this.Value = Value;
        }

        internal SqMetadataRef(long Block, long Offset)
        {
            Value = (Block << 16) | (Offset & 0xFFFF);
        }

        public long Block => (Value >> 16) & 0xFFFFFFFFFFFF;

        public int Offset => (int)(Value & 0xFFFF);
    }
}
