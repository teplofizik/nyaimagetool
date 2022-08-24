using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptArgs
    {
        public readonly int ArgConfig;
        public  readonly string[] RawArgs;

        public ScriptArgs(int ArgConfig, string[] RawArgs)
        {
            this.ArgConfig = ArgConfig;
            this.RawArgs = RawArgs;
        }
    }
}
