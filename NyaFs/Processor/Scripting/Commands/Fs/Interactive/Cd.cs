using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs.Interactive
{
    class Cd : ScriptStepGenerator
    {
        public Cd() : base("cd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { new Params.FsPathScriptArgsParam() }));
        }
        public override ScriptStep Get(ScriptArgs Args)
        {
            return new CdScriptStep(Args.RawArgs[0]);
        }

        public class CdScriptStep : ScriptStep
        {
            string Path;

            public CdScriptStep(string Path) : base("cd")
            {
                this.Path = Path;
            }

            private string CombinePath(string Base, string Name)
            {
                if ((Base == "/") || (Base == ".")) return Name;

                return Base + "/" + Name;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem not loaded");
                else
                {
                    if(Path.Length == 0)
                        return new ScriptStepResult(ScriptStepStatus.Error, "Specify path!");

                    if (Path == ".")
                        return new ScriptStepResult(ScriptStepStatus.Ok, Processor.ActivePath);
                    
                    var Item = Helper.FsHelper.GetItem(Fs, Processor.ActivePath, Path);

                    if(Item != null)
                    {
                        Processor.ActivePath = (Item.Filename.Length > 1) ?  "/" + Item.Filename : Item.Filename;
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Active directory is changed to {Processor.ActivePath}.");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, "Specified path is not found.");
                }
            }
        }
    }
}
