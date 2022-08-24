using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Types
{
    public enum Token
    {
        FDT_BEGIN_NODE = 1,
        FDT_END_NODE = 2,
        FDT_PROP = 3,
        FDT_NOP = 4,
        FDT_END = 9
    }
}
