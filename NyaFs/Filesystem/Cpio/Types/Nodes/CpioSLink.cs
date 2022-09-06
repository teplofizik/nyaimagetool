using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioSLink : CpioNode
    {
        public CpioSLink(string Path, uint Mode, uint Uid, uint Gid, string Target) : base(Path,
                                                                                           UTF8Encoding.UTF8.GetBytes(Target),
                                                                                           DateTime.Now,
                                                                                           Mode | Convert.ToUInt32(CpioModeFileType.C_ISLNK),
                                                                                           Uid,
                                                                                           Gid)
        {

        }

    }
}
