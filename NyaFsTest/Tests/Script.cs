using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{
    static class Script
    {
        public static void TestScriptFile(string FN)
        {
            var Processor = new NyaFs.Processor.ImageProcessor();
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, FN).Script;

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }

        public static void TestScript()
        {
            var Processor = new NyaFs.Processor.ImageProcessor();

            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, "test", new string[] {
               // "load initramfs.bin.SD ramfs legacy",
               // "load test.fit ramfs fit",
               // "load test.fit devtree fit",
              //  "load test.fit kernel fit",
                "load test.fit",
              //  "set ramfs name TestImageARM64",
              //  "set ramfs os linux",
              //  "set ramfs arch arm64",
                "set kernel load 1050000",
                "set kernel entry 1040000",
                "include include/scp.module",
                "file etc/test.txt test.txt rwxr--r-- 0 0",
                // "store initramfs.bin.SD.modified ramfs legacy"
                "store builded.fit",
                "store ramfs.cpio ramfs cpio"
            }).Script;

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
