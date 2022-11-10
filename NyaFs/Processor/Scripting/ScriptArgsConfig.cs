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

        protected bool CheckArgsLen(string[] Args) => (Params.Length == Args.Length);

        private bool HasVariable(string Arg)
        {
            return Variables.VariableChecker.IsCorrectName(Arg);
        }

        public virtual bool IsMyConfig(string[] Args)
        {
            if (!CheckArgsLen(Args)) return false;

            // Count ok. Check, is arg types correct
            for (int i = 0; i < Params.Length; i++)
            {
                var P = Params[i];
                var A = Args[i];

                if (HasVariable(A))
                    continue;
                
                if (!P.CheckParam(A))
                    return false;
            }

            return true;
        }

        public virtual bool CheckArgs(string[] Args) => CheckArgsLen(Args);
    }
}
