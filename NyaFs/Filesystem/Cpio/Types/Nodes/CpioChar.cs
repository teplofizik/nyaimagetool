using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioChar : CpioNode
    {
        public CpioChar(string Path, uint Mode, uint Uid, uint Gid, uint Major, uint Minor) : base(Path,
                                                                                                   new byte[] { },
                                                                                                   DateTime.Now,
                                                                                                   Mode | Convert.ToUInt32(CpioModeFileType.C_ISCHR),
                                                                                                   Uid,
                                                                                                   Gid)
        {
            this.Major = Major;
            this.Minor = Minor;
        }
    }
}
