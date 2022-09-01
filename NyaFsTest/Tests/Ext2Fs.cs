using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{

    static class Ext2Fs
    {
        public static void ReadSuperblock()
        {
            var Fs = new NyaFs.Filesystem.Ext2.Ext2Fs("zynq_ramfs.bin");

            Fs.Dump();
        }


        public static void DumpFs()
        {
            var Fs = new NyaFs.Filesystem.Ext2.Ext2Fs("ramdisk.image");

            DumpDir(Fs, ".");
        }

        private static void DumpDir(NyaFs.Filesystem.Ext2.Ext2Fs Fs, string Path)
        {
            var Elements = Fs.ReadDir(Path);

            foreach(var E in Elements)
            {
                Console.WriteLine(E);

                switch(E.NodeType)
                {
                    case NyaFs.Filesystem.Universal.Types.FilesystemItemType.Directory:
                        DumpDir(Fs, E.Path);
                        break;
                    case NyaFs.Filesystem.Universal.Types.FilesystemItemType.File:
                    case NyaFs.Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        {
                            var Content = Fs.Read(E.Path);

                            if(Content.Length != E.Size)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("File size is not equal to readed data...");
                                Console.ResetColor();
                            }
                        }
                        break;
                }
            }
        }
    }
}
