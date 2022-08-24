using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptStepResult
    {
        public readonly string Text;

        public readonly ScriptStepStatus Status;

        public ScriptStepResult(ScriptStepStatus Status, string Text)
        {
            this.Status = Status;
            this.Text = Text;
        }
    }
}
