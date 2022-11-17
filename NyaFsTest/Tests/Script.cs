using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{
    static class Script
    {
        public static void TestScriptFile(string FN)
        {
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Parser = new NyaFs.Processor.Scripting.ScriptParser(Base);
            var Processor = new NyaFs.Processor.ImageProcessor(Parser);

            var Script = Parser.ParseScript(FN);

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }

        public static void TestScript()
        {
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Parser = new NyaFs.Processor.Scripting.ScriptParser(Base);
            var Processor = new NyaFs.Processor.ImageProcessor(Parser);

            var Script = Parser.ParseScript("test", new string[] {
               // "load initramfs.bin.SD ramfs legacy",
               // "load test.fit ramfs fit",
               // "load test.fit devtree fit",
                "load rootfs.squashfs.hi3516ev100 ramfs squashfs",
              //  "load test.fit",
              //  "set ramfs name TestImageARM64",
              //  "set ramfs os linux",
              //  "set ramfs arch arm64",
              //  "load initramfs.zynq.SD ramfs legacy",
                //"set kernel load 1050000",
                //"set kernel entry 1040000",
              //  "include include/scp.module",
              //  "file etc/test.txt test.txt rwxr--r-- 0 0",
               // "store ramfs.cpio.bz2 ramfs bz2",
                // "store builded.fit",
               // "export exported",
             //   "store ramfs.cpio ramfs cpio"
            });

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
