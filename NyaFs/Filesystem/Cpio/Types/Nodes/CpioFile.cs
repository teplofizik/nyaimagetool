using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioFile : CpioNode
    {
        public CpioFile(string Path, uint Mode, uint Uid, uint Gid, byte[] Data) : base(Path,
                                                                           Data,
                                                                           DateTime.Now,
                                                                           Mode | Convert.ToUInt32(CpioModeFileType.C_ISREG),
                                                                           Uid,
                                                                           Gid)
        {

        }
    }
}
