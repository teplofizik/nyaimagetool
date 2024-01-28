using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Rm : ScriptStepGenerator
    {
        public Rm() : base("rm")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new RmScriptStep(Args.RawArgs[0]);
        }

        public class RmScriptStep : ScriptStep
        {
            string Path;

            public RmScriptStep(string Path) : base("rm")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if(Path.Contains('*'))
                {
                    if (Path.StartsWith("/"))
                    {
                        // Это путь с маской
                        var Items = Fs.Search(Path.Substring(1));

                        if(Items.Length > 0)
                        {
                            foreach(var I in Items)
                                Fs.Delete(I);

                            return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} deleted {Items.Length} items!");
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
                    }
                    else
                    {
                        var Dir = Fs.GetDirectory(Processor.ActivePath);
                        if(Dir != null)
                        {
                            var Items = Fs.Search(Dir, Path);
                            if (Items.Length > 0)
                            {
                                foreach (var I in Items)
                                    Fs.Delete(I);

                                return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} deleted {Items.Length} items!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"{Processor.ActivePath} not found!");
                    }
                }
                else
                { 
                    if (Fs.Exists(Path))
                    {
                        // Есть старый файл в файловой системе. Удалим.
                        Fs.Delete(Path);

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} deleted!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
                }
            }
        }
    }
}
