using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Types
{
    public enum FilesystemItemType
    {
        Unknown,
        File,
        Directory,
        SymLink,
        Character,
        Block,
        Fifo,
        Socket,
        HardLink // fs internal type, not exposed to user (romfs)...
    }
}
