using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioTrailer : CpioNode
    {
        public CpioTrailer() : base("TRAILER!!!",
                                                                 new byte[] { },
                                                                 DateTime.UnixEpoch,
                                                                 0x0000U,
                                                                 0, 0, 0, 0)
        {

        }
    }
}
