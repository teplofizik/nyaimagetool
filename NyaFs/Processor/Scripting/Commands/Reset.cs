using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Reset : ScriptStepGenerator
    {
        public Reset() : base("reset")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new ResetScriptStep();
        }

        public class ResetScriptStep : ScriptStep
        {
            public ResetScriptStep() : base("reset")
            {

            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                Processor.Reset();
                return new ScriptStepResult(ScriptStepStatus.Ok, "There are no loaded images now.");
            }
        }
    }
}
