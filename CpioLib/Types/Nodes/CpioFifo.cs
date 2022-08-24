using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioFifo : CpioNode
    {
        public CpioFifo(string Path, uint Major, uint Minor) : base(Path,
                                                                   new byte[] { },
                                                                   DateTime.Now,
                                                                   0x1180,
                                                                   Major,
                                                                   Minor)
        {

        }

    }
}
