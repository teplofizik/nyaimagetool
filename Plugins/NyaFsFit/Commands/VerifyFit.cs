using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Params = NyaFs.Processor.Scripting.Params;
using NyaFs.Processor.Scripting.Helper;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsFit.Commands
{
    public class VerifyFit : ScriptStepGenerator
    {
        public VerifyFit() : base("verifyfit")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new VerifyFitScriptStep(Args.RawArgs[0], Args.RawArgs[1]);

        public class VerifyFitScriptStep : ScriptStep
        {
            private readonly string Input;
            private readonly string Key;

            public VerifyFitScriptStep(string Input, string Key) : base("verifyfit")
            {
                this.Input = Input;
                this.Key = Key;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Filename = this.DetectFilePath(Input);
                if (Filename == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Input} not found!");
                if (!File.Exists(Key))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Key} not found!");

                try
                {
                    var Checks = FitSigner.Verify(File.ReadAllBytes(Filename), Key, out var Error);
                    if (Checks == null)
                        return new ScriptStepResult(ScriptStepStatus.Error, Error);

                    var Lines = new List<string>();
                    var Valid = true;
                    foreach (var Check in Checks)
                    {
                        Lines.Add($"{Check.Path}: {(Check.Valid ? "valid" : "invalid")}");
                        if (!Check.Valid)
                        {
                            Valid = false;
                            if (Check.Error != null)
                                Lines.Add($"  {Check.Error}");
                        }
                    }

                    return new ScriptStepResult(Valid ? ScriptStepStatus.Ok : ScriptStepStatus.Error,
                        string.Join(Environment.NewLine, Lines));
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, E.Message);
                }
            }
        }
    }
}
