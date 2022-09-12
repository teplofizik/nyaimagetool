using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class MetadataRef
    {
        public ulong MetadataOffset;
        public ulong UnpackedOffset;

        public MetadataRef(ulong Metadata, ulong Offset)
        {
            this.MetadataOffset = Metadata;
            this.UnpackedOffset = Offset;
        }

        public ulong Value => (MetadataOffset << 16) | (UnpackedOffset & 0xFFFF);
        public byte[] BytesValue
        {
            get
            {
                var Res = new byte[8];
                Res.WriteUInt64(0, Value);
                return Res;
            }
        }

        public Types.SqMetadataRef SqRef => new Types.SqMetadataRef(Convert.ToInt64(Value));
    }
}
