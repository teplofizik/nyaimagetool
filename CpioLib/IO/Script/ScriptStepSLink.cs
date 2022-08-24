using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.IO.Script
{
    class ScriptStepSLink : ScriptStep
    {
        public ScriptStepSLink(string Path, string To, string Mode, uint User, uint Group) : base("slink", $"{Path} {To} {Mode} {User} {Group}")
        {
            // TODO
        }
    }
}
