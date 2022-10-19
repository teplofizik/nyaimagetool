using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Var : ScriptStepGenerator
    {
        public Var() : base("var")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { 
                new Params.StringScriptArgsParam("name"),
                new Params.StringScriptArgsParam("value")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                new Params.StringScriptArgsParam("name") }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 0)
                return new VarScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
            else
                return new VarScriptStep(Args.RawArgs[0], "");
        }

        public class VarScriptStep : ScriptStep
        {
            private string VarName;
            private string Value;

            public VarScriptStep(string Name, string Value) : base("var")
            {
                VarName = Name;
                this.Value = Value;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                if (Variables.VariableChecker.IsCorrectName(VarName))
                {
                    Processor.Scope.SetValue(VarName, Value);

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, "Invalid variable name");
            }
        }
    }
}
