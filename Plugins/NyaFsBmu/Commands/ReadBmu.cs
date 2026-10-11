using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsBmu.Loader;
using NyaFsBmu.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsBmu.Commands
{
    class ReadBmu : ScriptStepGenerator
    {
        public ReadBmu() : base("readbmu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("part"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("destpath")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadBmuScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2]);
        }

        public class LoadBmuScriptStep : ScriptStep
        {
            private string Path;
            private string Part;
            private string Dest;

            public LoadBmuScriptStep(string Path, string Part, string Dest) : base("readbmu")
            {
                this.Path = Path;
                this.Part = Part;
                this.Dest = Dest;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new BmuImage(Data);

                if (Image.Correct)
                {
                    switch(Part)
                    {
                        case "kernel":
                            {
                                var Raw = Image.GetImageByType(BmuImageType.KERNEL);

                                if (Raw != null)
                                {
                                    System.IO.File.WriteAllBytes(Dest, Raw);
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported kernel from BMU");
                                }
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Error, $"No kernel part in BMU!");
                            }
                        case "ramfs":
                            {
                                var Raw = Image.GetImageByType(BmuImageType.RAMFS);

                                if (Raw != null)
                                {
                                    System.IO.File.WriteAllBytes(Dest, Raw);
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported ramfs from BMU");
                                }
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Error, $"No ramfs part in BMU!");
                            }
                        case "devtree":
                            {
                                var Raw = Image.GetImageByType(BmuImageType.DEVTREE);

                                if (Raw != null)
                                {
                                    System.IO.File.WriteAllBytes(Dest, Raw);
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported devtree from BMU");
                                }
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Error, $"No devtree part in BMU!");
                        }
                        case "uboot":
                            {
                                var Raw = Image.GetImageByType(BmuImageType.UBOOT);

                                if (Raw != null)
                                {
                                    System.IO.File.WriteAllBytes(Dest, Raw);
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported uboot from BMU");
                                }
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Error, $"No uboot part in BMU!");
                            }
                        case "composite":
                            {
                                var Raw = Image.GetImageByType(BmuImageType.COMPOSITE);

                                if (Raw != null)
                                {
                                    System.IO.File.WriteAllBytes(Dest, Raw);
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported composite from BMU");
                                }
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Error, $"No uboot part in BMU!");
                            }
                        default:
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown part of BMU! (kernel, ramfs, devtree, uboot)");
                    }


                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"BMU is invalid image!");
            }


        }
    }
}
