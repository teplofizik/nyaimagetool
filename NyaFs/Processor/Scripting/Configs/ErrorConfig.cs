using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Configs
{
    class AnyConfig : ScriptArgsConfig
    {
        Func<string, bool> ArgChecker;

        public AnyConfig(int Index) : base(Index, null)
        {
            ArgChecker = (A => true);
        }

        public AnyConfig(int Index, Func<string,bool> argchecker) : base(Index, null)
        {
            ArgChecker = argchecker;
        }

        public override bool IsMyConfig(string[] Args) => true;

        public override bool CheckArgs(string[] Args)
        {
            foreach(var A in Args)
            {
                if(!ArgChecker(A))
                    return false;
            }
            return true;
        }
    }
}
