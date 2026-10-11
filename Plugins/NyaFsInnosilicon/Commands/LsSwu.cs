using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsInnosilicon.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsInnosilicon.Commands
{
    class LsSwu : ScriptStepGenerator
    {
        public LsSwu() : base("lsswu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LsSwuScriptStep(Args.RawArgs[0]);
        }

        public class LsSwuScriptStep : ScriptStep
        {
            private string Path;

            public LsSwuScriptStep(string Path) : base("lsswu")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Fw = new InnoFirmwareUpdate(Data);
                var Root = Fw.ReadDir("/");
                if (Root.Length > 0)
                {
                    for (int i = 0; i < Root.Length; i++)
                    {
                        var Entry = Root[i];

                        Log.Write(0, $"[{i:00}] {Entry.ShortName} size {Entry.Size}");
                    }
                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Innosilicon image is readed!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Innosilicon image is invalid image!");
            }
        }
    }
}
