using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.IO.Script
{
    class ScriptStep
    {
        public readonly string Command;
        public readonly string Args;

        public ScriptStep(string Command, string Args)
        {
            this.Command = Command;
            this.Args = Args;
        }

        public string CommandLine => $"{Command} {Args}";
    }
}
