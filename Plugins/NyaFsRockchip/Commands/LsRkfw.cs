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
    class LsRkfw : ScriptStepGenerator
    {
        public LsRkfw() : base("lsrkfw")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadRkfwScriptStep(Args.RawArgs[0]);
        }

        public class LoadRkfwScriptStep : ScriptStep
        {
            private string Path;

            public LoadRkfwScriptStep(string Path) : base("lsrkfw")
            {
                this.Path = Path;
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
                        // File list
                        for(int i = 0; i < FW.FileCount; i++)
                        {
                            var Header = FW.GetHeader(i);

                            Log.Write(0, $"[{i:02}] {Header.Name}: {Header.FileName}  offset: {Header.RawOffset:X08}  nand: {Header.NandAddr:X08} img: {Header.ImgFSize:X08} orig: {Header.OrigFSize:X08}");
                        }

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Allwinner wty image is readed!");
                    }
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Rockchip image is invalid image!");
            }
        }
    }
}
