using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types
{
    internal enum SqInodeType
    {
        BasicDirectory = 1,
        BasicFile = 2,
        BasicSymlink = 3,
        BasicBlockDevice = 4,
        BasicCharDevice = 5,
        BasicFifo = 6,
        BasicSocket = 7,
        ExtendedDirectory = 8,
        ExtendedFile = 9,
        ExtendedSymlink = 10,
        ExtendedBlockDevice = 11,
        ExtendedCharDevice = 12,
        ExtendedFifo = 13,
        ExtendedSocket = 14
    }
}
