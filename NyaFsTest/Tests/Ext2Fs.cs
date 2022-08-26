using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{

    static class Ext2Fs
    {
        public static void ReadSuperblock()
        {
            var Fs = new NyaExt2.Implementations.Ext2Fs("ramfs.ext4");

            Fs.Dump();
            Fs.DumpINodes();
        }


    }
}
