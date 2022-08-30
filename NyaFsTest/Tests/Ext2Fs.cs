using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{

    static class Ext2Fs
    {
        public static void ReadSuperblock()
        {
            var Fs = new NyaExt2.Implementations.Ext2Fs("ext4/arm_ramdisk.ext4");

            Fs.Dump();
            Fs.DumpINodes();
        }


        public static void DumpFs()
        {
            var Fs = new NyaExt2.Implementations.Ext2Fs("ext4/ramfs.ext4");

            DumpDir(Fs, ".");
        }

        private static void DumpDir(NyaExt2.Implementations.Ext2Fs Fs, string Path)
        {
            var Elements = Fs.ReadDir(Path);

            foreach(var E in Elements)
            {
                Console.WriteLine(E);

                switch(E.NodeType)
                {
                    case NyaExt2.Types.FilesystemEntryType.Directory:
                        DumpDir(Fs, E.Path);
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Regular:
                    case NyaExt2.Types.FilesystemEntryType.Link:
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
