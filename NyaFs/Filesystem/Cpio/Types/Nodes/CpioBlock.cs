using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioBlock : CpioNode
    {
        public CpioBlock(string Path, uint Mode, uint Uid, uint Gid, uint Major, uint Minor) : base(Path,
                                                                     new byte[] { },
                                                                     DateTime.Now,
                                                                     Mode | Convert.ToUInt32(CpioModeFileType.C_ISBLK),
                                                                     Uid,
                                                                     Gid)
        {
            this.Major = Major;
            this.Minor = Minor;
        }

    }
}
