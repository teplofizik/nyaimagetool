using System;
using System.Collections.Generic;
using System.Text;

namespace NyaImageTool
{
    class InteractiveShell
    {
        NyaFs.Processor.ImageProcessor Processor = new NyaFs.Processor.ImageProcessor();
        NyaFs.Processor.Scripting.ScriptBase Base = new NyaFs.Processor.Scripting.ScriptBaseAll();

        public void ShellLoop()
        {
            while(true)
            {
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
            var Script = new NyaFs.Processor.Scripting.ScriptParser(Base, "cmd", new string[] { Line }).Script;
            Processor.Process(Script);
        }
    }
}
