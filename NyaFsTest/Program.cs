using NyaFsTest.Tests;
using System;
using System.IO;
using System.Text;
using System.Text.Unicode;

namespace NyaFsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //PackageTest.TestPackageSave();

            CreateSquashFs();
           // Ext2Fs.DumpFs();
        }

        static void CreateSquashFs()
        {
            var fs = new NyaFs.Filesystem.SquashFs.SquashFsBuilder(NyaFs.Filesystem.SquashFs.Types.SqCompressionType.Gzip);

            fs.Directory("/", 0, 0, 0777);
            fs.Directory("sau", 0, 0, 0777);
            fs.File("sau/file1.txt", UTF8Encoding.UTF8.GetBytes("Hola tío"), 0, 0, 0666);
            fs.File("file3.text", UTF8Encoding.UTF8.GetBytes("Hola tío2"), 0, 0, 0666);

            File.WriteAllBytes("test.sqfs", fs.GetFilesystemImage());
        }
    }
}
