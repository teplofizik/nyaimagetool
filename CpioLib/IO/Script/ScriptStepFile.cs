using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.IO.Script
{
    class ScriptStepFile : ScriptStep
    {
        public ScriptStepFile(string Path, string Local, string Mode, uint User, uint Group) : base("file", $"{Path} {Local} {Mode} {User} {Group}")
        {
            // TODO
        }
    }
}
