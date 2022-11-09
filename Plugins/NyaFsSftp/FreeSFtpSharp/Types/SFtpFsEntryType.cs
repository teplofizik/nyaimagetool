using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Types
{
    public enum SFtpFsEntryType
    {
        Unknown,
        File,
        Directory,
        SymLink,
        Character,
        Block,
        Fifo,
        Socket
    }
}
