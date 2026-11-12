using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Params = NyaFs.Processor.Scripting.Params;
using NyaFs.Processor.Scripting.Helper;
using System;
using System.IO;

namespace NyaFsFit.Commands
{
    public class SignFit : ScriptStepGenerator
    {
        public SignFit() : base("signfit")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam(),
                new Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new SignFitScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2],
                (Args.ArgConfig == 1) ? Args.RawArgs[3] : null);

        public class SignFitScriptStep : ScriptStep
        {
            private readonly string Input;
            private readonly string Output;
            private readonly string Key;
            private readonly string Control;

            public SignFitScriptStep(string Input, string Output, string Key, string Control) : base("signfit")
            {
                this.Input = Input;
                this.Output = Output;
                this.Key = Key;
                this.Control = Control;
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
                    var Signed = FitSigner.Sign(File.ReadAllBytes(Filename), Key, out var Count, out var Error);
                    if (Signed == null)
                        return new ScriptStepResult(ScriptStepStatus.Error, Error);

                    File.WriteAllBytes(Output, Signed);
                    if (!String.IsNullOrEmpty(Control))
                    {
                        using var Rsa = FitSigner.LoadKey(Key);
                        var ExistingControl = File.Exists(Control) ? File.ReadAllBytes(Control) : null;
                        var ControlData = NyaFs.ImageFormat.Composite.FitPublicKey.BuildControlDtb(
                            Rsa, System.IO.Path.GetFileNameWithoutExtension(Key), null, "conf", ExistingControl);
                        File.WriteAllBytes(Control, ControlData);
                    }
                    return new ScriptStepResult(ScriptStepStatus.Ok, $"FIT signed ({Count} signature node(s)): {Output}");
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, E.Message);
                }
            }
        }
    }
}
