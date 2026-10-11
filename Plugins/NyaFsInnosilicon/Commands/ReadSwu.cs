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
    class ReadSwu : ScriptStepGenerator
    {
        public ReadSwu() : base("readswu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("partname"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("dest")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new ReadSwuScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2]);
        }

        public class ReadSwuScriptStep : ScriptStep
        {
            private string Path;
            private string Partname;
            private string Dest;

            public ReadSwuScriptStep(string Path, string Partname, string Dest) : base("readrkfw")
            {
                this.Path = Path;
                this.Partname = Partname;
                this.Dest = Dest;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Fw = new InnoFirmwareUpdate(Data);
                if (Fw.Correct)
                {
                    var Raw = Fw.Read(Partname);
                    if (Raw != null)
                    {
                        try
                        {
                            System.IO.File.WriteAllBytes(Dest, Raw);
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Innosilicon image's partition {Partname} is exported to {Dest}!");
                        }
                        catch (Exception)
                        {
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot save partition with name {Partname} to file {Dest}!");
                        }
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Innosilicon image has no partition with name {Partname}!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Innosilicon image is invalid image!");
            }
        }
    }
}
