using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{
    public enum FilesystemItemType
    {
        File,
        Dir,
        SymLink,
        Node,
        Block,
        Fifo
    }
}
