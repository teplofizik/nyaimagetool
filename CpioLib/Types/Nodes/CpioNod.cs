using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioNod : CpioNode
    {
        public CpioNod(string Path, uint Major, uint Minor) : base(Path,
                                                                   new byte[] { },
                                                                   DateTime.Now,
                                                                   0x2192U,
                                                                   Major,
                                                                   Minor)
        {

        }

    }
}
