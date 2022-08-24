using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Params
{
    public class EnumScriptArgsParam : ScriptArgsParam
    {
        string[] Allowed;

        public EnumScriptArgsParam(string Name, string[] Allowed) : base(Name)
        {
            this.Allowed = Allowed;
        }

        public override bool CheckParam(string Arg) => Allowed.Contains(Arg);
    }
}
