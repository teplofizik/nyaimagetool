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
            {
                var Shell = new InteractiveShell();
                Shell.ShellLoop();
                ///Console.WriteLine("Usage: NyaFsTest <scriptfilename>");
            }
        }

        static void LoadScript(string FN)
        {
            var Processor = new NyaFs.Processor.ImageProcessor();
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, FN).Script;

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
