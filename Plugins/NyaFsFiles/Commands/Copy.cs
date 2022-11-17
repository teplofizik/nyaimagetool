using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsFiles.Commands
{
    class Copy : ScriptStepGenerator
    {

        public Copy() : base("copy")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new CopyScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
        }


        public class CopyScriptStep : ScriptStep
        {
            private string Filename;
            private string Dest;

            public CopyScriptStep(string Filename, string Dest) : base("copy")
            {
                this.Filename = Filename;
                this.Dest = Dest;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                try
                {
                    System.IO.File.Copy(Filename, Dest);
                        return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot copy '{Filename} to {Dest}: {E.Message}'");
                }
            }
        }
    }
}
