using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.IO.Script
{
    class ScriptStepDir : ScriptStep
    {
        public ScriptStepDir(string Path, string Mode, uint User, uint Group) : base("dir", $"{Path} {Mode} {User} {Group}")
        {
            // TODO
        }
    }
}
