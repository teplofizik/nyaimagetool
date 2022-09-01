using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Ext2.Types
{
    /// <summary>
    /// 0x1000  S_IFIFO(FIFO)
    /// 0x2000  S_IFCHR(Character device)
    /// 0x4000  S_IFDIR(Directory)
    /// 0x6000  S_IFBLK(Block device)
    /// 0x8000  S_IFREG(Regular file)
    /// 0xA000  S_IFLNK(Symbolic link)
    /// 0xC000  S_IFSOCK(Socket)
    /// </summary>
    internal enum ExtINodeType
    {
        NONE = 0x0000,
        FIFO = 0x1000,
        CHAR = 0x2000,
        DIR = 0x4000,
        BLOCK = 0x6000,
        REG = 0x8000,
        LINK = 0xA000,
        SOCK = 0xC000
    }
}
