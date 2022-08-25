using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{
    static class Ext4Fs
    {
        public static void ReadSuperblock()
        {
            var Fs = new NyaExt4.ExtFs("ramfs.ext4");

        }
    }
}
