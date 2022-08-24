using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Params
{
    class ModeScriptArgsParam : ScriptArgsParam
    {
        public ModeScriptArgsParam() : base("mode")
        {

        }

        public override bool CheckParam(string Arg)
        {
            return Utils.CheckMode(Arg);
        }
    }
}
