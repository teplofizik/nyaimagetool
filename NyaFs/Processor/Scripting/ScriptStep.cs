using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptStep
    {
        public string ScriptFilename;
        public string ScriptName;
        public int ScriptLine;

        public readonly string Name;

        private Conditions.Condition Condition = null;

        private ScriptStepGenerator Generator;
        private string[] GeneratorArgs;

        public ScriptStep(string Name)
        {
            this.Name = Name;
        }

        private string PreprocessArg(Variables.VariableScope Scope, string Arg)
        {
            if (Variables.VariableChecker.IsCorrectName(Arg) && Scope.IsDefined(Arg))
                return Scope.GetValue(Arg);
            else
                return Arg;
        }

        private string[] PreprocessArgs(Variables.VariableScope Scope, string[] Args)
        {
            return Array.ConvertAll(Args, A => PreprocessArg(Scope, A));
        }

        public ScriptStep GetPreprocessed(Variables.VariableScope Scope)
        {
            var SArgs = Generator.GetArgs(PreprocessArgs(Scope, GeneratorArgs));
            if (SArgs != null)
            {
                var Step = Generator.Get(SArgs);

                Step.SetScriptInfo(ScriptFilename, ScriptName, ScriptLine);
                Step.SetGenerator(Generator, GeneratorArgs);
                if (Condition != null) 
                    Step.SetCondition(Condition);

                return Step;
            }
            else
            {
                Log.Error(0, $"Error at {ScriptName}:{ScriptLine}. Invalid arguments for command '{Name}'.");
                return null;
            }
        }

        public void SetGenerator(ScriptStepGenerator Gen, string[] Args)
        {
            Generator = Gen;
            GeneratorArgs = Args;
        }

        public void SetScriptInfo(string Filename, string Name, int Line)
        {
            ScriptFilename = Filename;
            ScriptName = Name;
            ScriptLine = Line;
        }

        public void SetCondition(Conditions.Condition Condition)
        {
            this.Condition = Condition;
        }

        public bool CheckCondition(ImageProcessor Proc)
        {
            if (Condition != null)
                return Condition.IsCorrect(Proc);
            else
                return true;
        }

        public string ScriptPath => System.IO.Path.GetDirectoryName(ScriptFilename);

        public virtual ScriptStepResult Exec(ImageProcessor Processor)
        {
            return new ScriptStepResult(ScriptStepStatus.Error, "Override default Exec function");
        }
    }
}
