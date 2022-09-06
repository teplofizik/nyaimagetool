using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioFifo : CpioNode
    {
        public CpioFifo(string Path, uint Mode, uint Uid, uint Gid) : base(Path, 
                                                                           new byte[] { }, 
                                                                           DateTime.Now,
                                                                           Mode | Convert.ToUInt32(CpioModeFileType.C_ISFIFO),
                                                                           Uid,
                                                                           Gid)
        {

        }

    }
}
