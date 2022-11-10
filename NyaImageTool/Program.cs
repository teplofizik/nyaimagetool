using System;

namespace NyaImageTool
{
    class Program
    {
        static string[] GetParams(string[] args)
        {
            var res = new string[args.Length];
            for (int i = 0; i < args.Length - 1; i++)
                res[i] = args[i + 1];

            return res;
        }

        static void Main(string[] args)
        {
            if (args.Length >= 1)
                LoadScript(args[0], GetParams(args));
            else
            {
                var Shell = new InteractiveShell();
                Shell.ShellLoop();
                ///Console.WriteLine("Usage: NyaFsTest <scriptfilename>");
            }
        }

        static void LoadScript(string FN, string[] Params)
        {
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Parser = new NyaFs.Processor.Scripting.ScriptParser(Base);
            var Processor = new NyaFs.Processor.ImageProcessor(Parser);

            var Script = Parser.ParseScript(FN);

            // TODO:
            foreach (var P in Params)
            {
                if (NyaFs.Processor.Scripting.Variables.VariableChecker.IsCorrectName(P))
                {
                    Processor.Scope.SetValue(P, "");
                }
            }

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
