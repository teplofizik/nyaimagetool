using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Params = NyaFs.Processor.Scripting.Params;
using NyaFsFit;
using NyaFs.Processor.Scripting.Helper;
using System;

namespace NyaFsFit.Commands
{
    public class ExportFit : ScriptStepGenerator
    {
        public ExportFit() : base("exportfit")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.LocalPathScriptArgsParam(),
                new Params.StringScriptArgsParam("outdir")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new ExportFitScriptStep(Args.RawArgs[0], Args.RawArgs[1]);

        public class ExportFitScriptStep : ScriptStep
        {
            private readonly string Path;
            private readonly string OutDir;

            public ExportFitScriptStep(string Path, string OutDir) : base("exportfit")
            {
                this.Path = Path;
                this.OutDir = OutDir;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Filename = this.DetectFilePath(Path);
                if (Filename == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, Path + " not found!");

                string Error;
                if (!new FitSourceExporter().Export(Filename, OutDir, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, "FIT source exported to " + OutDir);
            }
        }
    }
}
