using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class SystemCmd : ScriptStepGenerator
    {
        public SystemCmd() : base("system")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { 
                new Params.StringScriptArgsParam("command")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new SystemScriptStep(Args.RawArgs[0]);
        }

        public class SystemScriptStep : ScriptStep
        {
            private string Cmd;

            public SystemScriptStep(string Cmd) : base("system")
            {
                this.Cmd = Cmd;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Res = System.Diagnostics.Process.Start("cmd.exe", $"/C {Cmd}");

                Res.WaitForExit();
                if (Res.ExitCode == 0)
                {
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Program return error code {Res.ExitCode}");
            }
        }
    }
}
