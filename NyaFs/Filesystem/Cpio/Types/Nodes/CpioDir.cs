using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types.Nodes
{
    public class CpioDir : CpioNode
    {
        public CpioDir(string Path, uint Mode, uint Uid, uint Gid) : base(Path, 
                                                                          new byte[] { },
                                                                          DateTime.Now,
                                                                          Mode | Convert.ToUInt32(CpioModeFileType.C_ISDIR),
                                                                          Uid,
                                                                          Gid)
        {

        }

        private static DateTime GetDirectoryInfo(string Dir)
        {
            if (Dir != null)
                return new DirectoryInfo(Dir).LastWriteTime;
            else
                return DateTime.Now;
        }

    }
}
