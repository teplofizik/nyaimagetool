using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class FragmentBlock
    {
        byte[] Content;
        long Filled = 0;
        long BlockOffset = 0;

        public FragmentBlock(long Offset, uint Size)
        {
            BlockOffset = Offset;
            Content = new byte[Size];
        }

        public byte[] FullData => Content;
        public byte[] Data => Content.ReadArray(0, Filled);

        public long DataSize => Filled;

        /// <summary>
        /// Write part of fragment to fragment block
        /// </summary>
        /// <param name="Data">Data to write</param>
        /// <param name="Offset">Start offset in data</param>
        /// <returns>Reference to fragment</returns>
        public MetadataRef Write(byte[] Data, ref long Offset)
        {
            var Res = new MetadataRef(Convert.ToUInt64(BlockOffset), Convert.ToUInt64(Filled));
            var Size = Data.Length - Offset;

            var FreeSpace = Content.Length - Filled;
            if (Size > FreeSpace)
            {
                Array.Copy(Data, Offset, Content, Filled, FreeSpace);
                Filled += FreeSpace;
                Offset += FreeSpace;
            }
            else
            {
                Array.Copy(Data, Offset, Content, Filled, Size);
                Filled += Size;
                Offset += Size;
            }

            return Res;
        }

        public bool IsFilled => Filled == Content.Length;
    }
}
