using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptStepGenerator
    {
        public readonly string Name;
        private List<ScriptArgsConfig> Configs = new List<ScriptArgsConfig>();

        public ScriptStepGenerator(string Name)
        {
            this.Name = Name;
        }

        protected void AddConfig(ScriptArgsConfig Config)
        {
            Configs.Add(Config);
        }

        public virtual ScriptStep Get(ScriptArgs Args)
        {
            throw new NotImplementedException("There is need to implement script step getter");
        }

        public ScriptArgs GetArgs(string[] Args)
        {
            foreach(var C in Configs)
            {
                if(CheckConfig(C, Args))
                    return new ScriptArgs(C.Id, Args);
            }

            return null;
        }

        private bool CheckConfig(ScriptArgsConfig Cfg, string[] Args)
        {
            // Check args count
            if (Cfg.Params.Length != Args.Length) return false;

            // Count ok. Check, is arg types correct
            for(int i = 0; i < Cfg.Params.Length; i++)
            {
                var P = Cfg.Params[i];
                var A = Args[i];

                if(!P.CheckParam(A))
                    return false;
            }
            return true;
        }
    }
}
