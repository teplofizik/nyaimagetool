using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsRockchip.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsRockchip.Commands
{
    class LoadRkfw : ScriptStepGenerator
    {
        public LoadRkfw() : base("readrkfw")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("partname"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("dest")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new ReadRkfwScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2]);
        }

        public class ReadRkfwScriptStep : ScriptStep
        {
            private string Path;
            private string Partname;
            private string Dest;

            public ReadRkfwScriptStep(string Path, string Partname, string Dest) : base("readrkfw")
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
                var Image = new RkfwImage(Data);

                if (Image.Correct)
                {
                    var FW = Image.FirmwareData;
                    if (FW.Correct)
                    {
                        var Raw = FW.GetPartition(Partname);
                        if (Raw != null)
                        {
                            try
                            {
                                File.WriteAllBytes(Dest, Raw);
                                return new ScriptStepResult(ScriptStepStatus.Ok, $"Allwinner wty image's partition {Partname} is exported to {Dest}!");
                            }
                            catch (Exception E)
                            {
                                return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot save partition with name {Partname} to file {Dest}!");
                            }
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Allwinner wty image has no partition with name {Partname}!");
                    }
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Rockchip image is invalid image!");
            }
        }
    }
}
