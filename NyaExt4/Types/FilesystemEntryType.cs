using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types
{
    public enum FilesystemEntryType
    {
        Invalid,
        Fifo,
        Character,
        Directory,
        Block,
        Regular,
        Link,
        Socket
    }
}
