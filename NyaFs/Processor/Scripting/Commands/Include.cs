using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Include : ScriptStepGenerator
    {
        public Include() : base("include")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                new Params.LocalPathScriptArgsParam() 
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new IncludeScriptStep(Args.RawArgs[0]);
        }


        public class IncludeScriptStep : ScriptStep
        {
            private string Path;

            public IncludeScriptStep(string Path) : base("include")
            {
                this.Path = Path;
            }

            private string DetectPath(string Caller, string Path)
            {
                string[] Variants = new string[]
                {
                    "",
                    System.IO.Path.GetDirectoryName(Caller)
                };
                foreach (var V in Variants)
                {
                    var Target = System.IO.Path.Combine(V, Path);

                    if (System.IO.File.Exists(Target))
                        return Target;
                }

                return null;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var IncludePath = DetectPath(ScriptFilename, Path);
                if (IncludePath == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found");
                else
                {
                    var Script = Processor.Parser.Parse(Path, System.IO.Path.GetFileName(Path), System.IO.File.ReadAllLines(Path));
                    if(Script.HasErrors)
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Errors in script");
                    else
                    {
                        Processor.Process(Script);
                        return new ScriptStepResult(ScriptStepStatus.Ok, null);
                    }
                }
            }
        }
    }
}
