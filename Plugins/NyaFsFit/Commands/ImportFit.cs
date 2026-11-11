using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Helper;
using Params = NyaFs.Processor.Scripting.Params;
using NyaFsFit;
using System;

namespace NyaFsFit.Commands
{
    public class ImportFit : ScriptStepGenerator
    {
        public ImportFit() : base("importfit")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.LocalPathScriptArgsParam(),
                new Params.StringScriptArgsParam("outfile")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new ImportFitScriptStep(Args.RawArgs[0], Args.RawArgs[1]);

        public class ImportFitScriptStep : ScriptStep
        {
            private readonly string Path;
            private readonly string OutFile;

            public ImportFitScriptStep(string Path, string OutFile) : base("importfit")
            {
                this.Path = Path;
                this.OutFile = OutFile;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Filename = this.DetectFilePath(Path);
                if (Filename == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, Path + " not found!");

                string Error;
                if (!new FitSourceImporter().Import(Filename, OutFile, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, "FIT image written to " + OutFile);
            }
        }
    }
}
