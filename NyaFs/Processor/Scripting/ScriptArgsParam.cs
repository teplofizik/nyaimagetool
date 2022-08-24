using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptArgsParam
    {
        public readonly string Name;

        public ScriptArgsParam(string Name)
        {
            this.Name = Name;
        }

        public virtual bool CheckParam(string Arg)
        {
            return true;
        }
    }
}
