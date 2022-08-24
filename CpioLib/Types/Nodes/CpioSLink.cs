using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioSLink : CpioNode
    {
        public CpioSLink(string Path, string ToPath) : base(Path,
                                                            UTF8Encoding.UTF8.GetBytes(ToPath),
                                                            DateTime.Now,
                                                            0xA1A4U)
        {

        }

    }
}
