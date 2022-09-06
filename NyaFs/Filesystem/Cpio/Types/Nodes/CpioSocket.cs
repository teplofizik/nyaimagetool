using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioSocket : CpioNode
    {
        public CpioSocket(string Path, uint Mode, uint Uid, uint Gid) : base(Path, 
                                           new byte[] { }, 
                                           DateTime.Now,
                                           Mode | Convert.ToUInt32(CpioModeFileType.C_ISSOCK),
                                           Uid,
                                           Gid)
        {

        }
    }
}
