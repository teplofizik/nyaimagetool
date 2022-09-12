using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class MetadataWriter
    {
        List<byte> Dst;
        uint BlockSize;
        ulong Offset;

        FragmentBlock TempMetablock;
        Compression.BaseCompressor Compressor;

        public MetadataWriter(List<byte> Dst, ulong Offset, uint BlockSize, Compression.BaseCompressor Compressor)
        {
            this.Dst = Dst;
            this.Offset = Offset;
            this.BlockSize = BlockSize;
            this.Compressor = Compressor;

            TempMetablock = new FragmentBlock(0, BlockSize);
        }

        private byte[] CompressBlock(byte[] Data)
        {
            var Compressed = Compressor?.Compress(Data) ?? Data;

            if (Compressed.Length >= Data.Length)
            {
                var Res = new byte[Data.Length + 2];
                Res.WriteUInt16(0, Convert.ToUInt32(Data.Length) | 0x8000);
                Res.WriteArray(2, Data, Data.Length);

                return Res;
            }
            else
            {
                var Res = new byte[Compressed.Length + 2];
                Res.WriteUInt16(0, Convert.ToUInt32(Compressed.Length));
                Res.WriteArray(2, Compressed, Compressed.Length);

                return Res;
            }
        }

        public MetadataRef Write(byte[] Data)
        {
            long Offset = 0;
            var Ref = TempMetablock.Write(Data, ref Offset);

            while (Offset < Data.Length)
            {
                if (TempMetablock.IsFilled)
                {
                    Dst.AddRange(CompressBlock(TempMetablock.Data));

                    TempMetablock = new FragmentBlock(Dst.Count, BlockSize);
                }

                TempMetablock.Write(Data, ref Offset);
            }

            Ref.MetadataOffset += this.Offset;
            return Ref;
        }

        public void Flush()
        {
            if (TempMetablock.DataSize > 0)
                Dst.AddRange(CompressBlock(TempMetablock.Data));
        }
    }
}
