using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptArgsConfig
    {
        public readonly int Id;
        public readonly ScriptArgsParam[] Params;

        public ScriptArgsConfig(int Id, ScriptArgsParam[] Params)
        {
            this.Id = Id;
            this.Params = Params;
        }
    }
}
