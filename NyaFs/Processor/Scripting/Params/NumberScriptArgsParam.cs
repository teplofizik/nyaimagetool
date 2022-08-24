using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Params
{
    class NumberScriptArgsParam : ScriptArgsParam
    {
        public NumberScriptArgsParam(string Name) : base(Name)
        {

        }

        public override bool CheckParam(string Arg)
        {
            uint Res;

            return UInt32.TryParse(Arg, out Res);
        }
    }
}
