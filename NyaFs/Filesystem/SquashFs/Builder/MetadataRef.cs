using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class MetadataRef
    {
        public long MetadataOffset;
        public long UnpackedOffset;

        public MetadataRef(long Metadata, long Offset)
        {
            this.MetadataOffset = Metadata;
            this.UnpackedOffset = Offset;
        }
    }
}
