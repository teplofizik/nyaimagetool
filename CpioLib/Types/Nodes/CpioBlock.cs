using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioBlock : CpioNode
    {
        public CpioBlock(string Path, uint Major, uint Minor) : base(Path,
                                                                   new byte[] { },
                                                                   DateTime.Now,
                                                                   0x61b0u,
                                                                   Major,
                                                                   Minor)
        {

        }

    }
}
