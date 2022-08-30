using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Export : ScriptStepGenerator
    {
        public Export() : base("export")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new Params.LocalPathScriptArgsParam()
                }));

        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            return new ExportScriptStep(Args.RawArgs[0]);
        }

        public class ExportScriptStep : ScriptStep
        {
            string Path;

            public ExportScriptStep(string Path) : base("export")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();

                if ((Fs == null) || (Fs.Loaded == false))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Filesystem is not loaded!");

                if (!System.IO.Directory.Exists(Path))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(Path);
                    }
                    catch(Exception E)
                    {
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Could not make target directory for filesystem export!");
                    }
                }

                var Exporter = new NyaFs.ImageFormat.Elements.Fs.Writer.NativeWriter(Path);
                Exporter.WriteFs(Fs);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Filesystem is exported to dir {Path}!");
            }
        }
    }
}
