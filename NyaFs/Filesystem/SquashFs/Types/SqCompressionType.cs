using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    internal enum SqCompressionType
    {
        Gzip = 1,
        Lzma = 2,
        Lzo = 3,
        Xz = 4,
        Lz4 = 5,
        Zstd = 6
    }
}
