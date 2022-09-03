using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NyaLZO
{
    /// <summary>
    ///  MiniLZO-based decompressor
    /// </summary>
    public class LZO1xDecompressor
    {
        public static byte[] Decompress(byte[] Src, uint BlockSize)
        {
            var State = new LZODecState(Src, BlockSize);

            return State.Decompress();
        }

    }
}
