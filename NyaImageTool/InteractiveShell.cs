using System;
using System.Collections.Generic;
using System.Text;

namespace NyaImageTool
{
    class InteractiveShell
    {
        NyaFs.Processor.ImageProcessor Processor;

        public InteractiveShell()
        {
            var Base = new NyaFs.Processor.Scripting.ScriptBaseInteractive();
            var Parser = new NyaFs.Processor.Scripting.ScriptParser(Base);

            Processor = new NyaFs.Processor.ImageProcessor(Parser);
        }

        public void ShellLoop()
        {
            while(true)
            {
                if(Processor.IsFsLoaded)
                    Console.Write($"Nya:{Processor.ActivePath}> ");
                else
                    Console.Write("Nya> ");

                var Readed = Console.ReadLine();

                if ((Readed == "quit") || (Readed == "exit"))
                    break;

                // Parse command:
                RunCommand(Readed);
            }
        }

        private void RunCommand(string Line)
        {
            var Script = Processor.Parser.ParseScript("cmd", new string[] { Line });

            Processor.Process(Script);
        }
    }
}
