using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Chmod : ScriptStepGenerator
    {
        public Chmod() : base("chmod")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.FsPathScriptArgsParam(),
                new Params.ModeScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new ChmodScriptStep(Args.RawArgs[0], Utils.ConvertMode(Args.RawArgs[1]));
        }

        public class ChmodScriptStep : ScriptStep
        {
            string Path;
            uint Mode;

            public ChmodScriptStep(string Path, uint Mode) : base("chmod")
            {
                this.Path = Path;
                this.Mode = Mode;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы

                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (Fs.Exists(Path))
                {
                    // Есть старый файл в файловой системе. Удалим.
                    var Item = Fs.GetElement(Path);
                    Item.Mode = Mode;

                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Mode of {Path} is updated to {Utils.ConvertModeToString(Mode)}!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
            }
        }
    }
}

