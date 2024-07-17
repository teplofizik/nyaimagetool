using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

        static string[] GetAppPluginsDirs()
        {
            var Res = new List<string>();
            Res.Add("plugins");

            var AppPath = Assembly.GetEntryAssembly().CodeBase;
            if(AppPath != null)
            {
                var AppDir = Path.GetDirectoryName(AppPath);
                if(AppDir != null)
                {
                    Res.Add(Path.Combine(AppDir, "plugins"));
                }
            }

            return Res.ToArray();
        }

        static void LoadScript(string FN, string[] Params)
        {
            var Base = new NyaFs.Processor.Scripting.ScriptBaseAll();
            var Parser = new NyaFs.Processor.Scripting.ScriptParser(Base);
            var Processor = new NyaFs.Processor.ImageProcessor(Parser, GetAppPluginsDirs());

            var Script = Parser.ParseScript(FN);

            // TODO:
            foreach (var P in Params)
            {
                if (P == null) continue;

                if (NyaFs.Processor.Scripting.Variables.VariableChecker.IsCorrectName(P))
                    Processor.Scope.SetValue(P, "");
                else
                {
                    var RepP = P.Replace('%', '$');
                    if (NyaFs.Processor.Scripting.Variables.VariableChecker.IsCorrectName(RepP))
                        Processor.Scope.SetValue(RepP, "");
                }
            }

            if (!Script.HasErrors)
                Processor.Process(Script);
            else
                Console.WriteLine("Errors in script.");
        }
    }
}
