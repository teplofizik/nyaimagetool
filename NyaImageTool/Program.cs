using System;

namespace NyaImageTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
                LoadScript(args[0]);
            else
                Console.WriteLine("Usage: NyaFsTest <scriptfilename>");
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
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.Chown());
            B.Add(new NyaFs.Processor.Scripting.Commands.Fs.Chmod());

            return B;
        }

        static void LoadScript(string FN)
        {
            var Processor = new NyaFs.Processor.ImageProcessor();
            var Base = GetBase();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, FN).Script;

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
