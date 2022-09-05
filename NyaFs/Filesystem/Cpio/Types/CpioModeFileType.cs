using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types
{
    public enum CpioModeFileType {
        C_ISDIR = 0x4000, // Directory
        C_ISFIFO = 0x1000, // FIFO
        C_ISREG = 0x8000, // Regular file
        C_ISBLK = 0x6000, // Block special
        C_ISCHR = 0x2000, // Character special
        C_ISCTG = 0x9000, // Reserved
        C_ISLNK = 0xA000, // Symbolic link.
        C_ISSOCK = 0xC000, // Socket
    }
}
