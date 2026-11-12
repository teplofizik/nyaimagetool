using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsBmu.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsBmu.Commands
{
    class VerifyBmu : ScriptStepGenerator
    {
        public VerifyBmu() : base("verifybmu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new VerifyBmuScriptStep(Args.RawArgs[0], (Args.ArgConfig == 1) ? Args.RawArgs[1] : null);

        public class VerifyBmuScriptStep : ScriptStep
        {
            private readonly string Path;
            private readonly string RootKeyPath;

            public VerifyBmuScriptStep(string Path, string RootKeyPath) : base("verifybmu")
            {
                this.Path = Path;
                this.RootKeyPath = RootKeyPath;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                if (!File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");
                if ((RootKeyPath != null) && !File.Exists(RootKeyPath))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{RootKeyPath} not found!");

                var RootPem = (RootKeyPath != null) ? File.ReadAllText(RootKeyPath) : null;
                var Result = BmuSecurity.VerifySingle(File.ReadAllBytes(Path), RootPem);
                var Lines = new List<string>
                {
                    $"BMU signature: {(Result.Valid ? "valid" : "invalid")}",
                    $"Miner key fingerprint: {Result.KeyFingerprint ?? "?"}",
                    $"Miner key signature: {(Result.MinerKeySignatureValid.HasValue ? (Result.MinerKeySignatureValid.Value ? "valid" : "invalid") : "not checked")}"
                };

                if (Result.RootKeyFingerprint != null)
                    Lines.Add($"Root key fingerprint: {Result.RootKeyFingerprint}");
                if (!string.IsNullOrEmpty(Result.Error))
                    Lines.Add($"Error: {Result.Error}");

                foreach (var File in Result.Files)
                    Lines.Add($"{File.Name}: {(File.Valid ? "valid" : "invalid")}");

                Lines.Add($"Overall signature: {(Result.OverallSignatureValid ? "valid" : "invalid")}");
                return new ScriptStepResult(Result.Valid ? ScriptStepStatus.Ok : ScriptStepStatus.Error,
                    string.Join(Environment.NewLine, Lines));
            }
        }
    }
}
