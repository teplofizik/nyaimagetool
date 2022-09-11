using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class FragmentBlock
    {
        byte[] Content;
        long Filled = 0;
        long BlockOffset;

        public FragmentBlock(long Offset, uint Size)
        {
            BlockOffset = Offset;
            Content = new byte[Size];
        }

        public byte[] Data => Content;

        public long DataSize => Filled;

        /// <summary>
        /// Write part of fragment to fragment block
        /// </summary>
        /// <param name="Data">Data to write</param>
        /// <param name="Offset">Start offset in data</param>
        /// <returns>Reference to fragment</returns>
        public MetadataRef Write(byte[] Data, ref long Offset)
        {
            var Res = new MetadataRef(BlockOffset, Filled);
            var Size = Data.Length - Offset;

            var FreeSpace = Content.Length - BlockOffset;

            if (Size > FreeSpace)
            {
                Array.Copy(Data, Offset, Content, BlockOffset, FreeSpace);
                BlockOffset += FreeSpace;
                Offset += FreeSpace;
            }
            else
            {
                Array.Copy(Data, Offset, Content, BlockOffset, Size);
                BlockOffset += Size;
                Offset += Size;
            }

            return Res;
        }

        public bool IsFilled => BlockOffset == Content.Length;
    }
}
