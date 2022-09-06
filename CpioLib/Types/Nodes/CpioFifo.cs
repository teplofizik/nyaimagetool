using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioFifo : CpioNode
    {
        public CpioFifo(string Path) : base(Path,
                                                                   new byte[] { },
                                                                   DateTime.Now,
                                                                   0x1180)
        {

        }

    }
}
