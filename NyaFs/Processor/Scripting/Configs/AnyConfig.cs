using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Configs
{
    class ErrorConfig : ScriptArgsConfig
    {
        private string Message = "";

        public ErrorConfig() : base(-1, null)
        {

        }

        public ErrorConfig(string Message) : base(-1, null)
        {
            this.Message = Message;
        }

        public override bool IsMyConfig(string[] Args) => true;

        public override bool CheckArgs(string[] Args)
        {
            var M = Message + "";

            for(int i = 1; i < Args.Length; i++)
            {
                M = M.Replace($"%{i}%", Args[i]);
            }

            Log.Error(0, M);
            return false;
        }
    }
}
