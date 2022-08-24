using System;

namespace NyaFsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
                TestScriptFile(args[0]);
            else
                Console.WriteLine("Usage: NyaFsTest <scriptfilename>");
//            TestScript();
        }

        static NyaFs.Processor.Scripting.ScriptBase GetBase()
        {
            var B = new NyaFs.Processor.Scripting.ScriptBase();
            B.Add(new NyaFs.Processor.Scripting.Commands.Load());
            B.Add(new NyaFs.Processor.Scripting.Commands.Store());
            B.Add(new NyaFs.Processor.Scripting.Commands.Set());
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.Dir());
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.File());
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.SLink());
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.Rm());

            return B;
        }

        static void TestScriptFile(string FN)
        {
            var Processor = new NyaFs.Processor.ImageProcessor();
            var Base = GetBase();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, FN).Script;

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }

        static void TestScript()
        {
            var Processor = new NyaFs.Processor.ImageProcessor();

            var Base = GetBase();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, "test", new string[] {
               // "load initramfs.bin.SD ramfs legacy",
               // "load test.fit ramfs fit",
               // "load test.fit devtree fit",
              //  "load test.fit kernel fit",
                "load test.fit",
              //  "set ramfs name TestImageARM64",
              //  "set ramfs os linux",
              //  "set ramfs arch arm64",
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
