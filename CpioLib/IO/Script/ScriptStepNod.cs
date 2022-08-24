using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.IO.Script
{
    class ScriptStepNod : ScriptStep
    {
        public ScriptStepNod(string Path, string Mode, uint User, uint Group, string Type, uint Maj, uint Min) :
            base("nod", $"{Path} {Mode} {User} {Group} {Type} {Maj} {Min}")
        {
            // TODO
        }
    }
}
