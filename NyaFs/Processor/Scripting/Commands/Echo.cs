using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Echo : ScriptStepGenerator
    {
        public Echo() : base("echo")
        {
            AddConfig(new ScriptArgsConfig(0, null));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new EchoScriptStep(Args.RawArgs);
        }

        public class EchoScriptStep : ScriptStep
        {
            private string[] Args;

            public EchoScriptStep(string[] Args) : base("echo")
            {
                this.Args = Args;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                Log.Write(0, String.Join(" ", Args));

                return new ScriptStepResult(ScriptStepStatus.Ok, null);
            }
        }
    }
}
