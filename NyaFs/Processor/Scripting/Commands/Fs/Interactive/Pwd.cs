using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs.Interactive
{
    class Pwd : ScriptStepGenerator
    {
        public Pwd() : base("pwd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }
        public override ScriptStep Get(ScriptArgs Args)
        {
            return new PwdScriptStep();
        }

        public class PwdScriptStep : ScriptStep
        {
            public PwdScriptStep() : base("pwd")
            {

            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if ((Fs == null) || !Fs.Loaded)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem not loaded");
                else
                    return new ScriptStepResult(ScriptStepStatus.Ok, Processor.ActivePath);
            }
        }
    }
}
