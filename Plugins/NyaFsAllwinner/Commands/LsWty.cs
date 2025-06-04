using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAllwinner.Types;
using NyaFsAllwinner.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsAllwinner.Commands
{
    class LsWty : ScriptStepGenerator
    {
        public LsWty() : base("lswty")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LsWtyScriptStep(Args.RawArgs[0]);
        }

        public class LsWtyScriptStep : ScriptStep
        {
            private string Path;

            public LsWtyScriptStep(string Path) : base("lswty")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new WtyImage(Data);

                if (Image.Correct)
                {
                    var Files = Image.FileHeaders;

                    // File list
                    for (int i = 0; i < Files.Length; i++)
                    {
                        var Header = Files[i];

                        Log.Write(0, $"[{i:00}] {Header.Maintype} ({Header.Subtype}): {Header.Filename}  offset: {Header.Offset:X08} img: {Header.StoredLength:X08} orig: {Header.OriginalLength:X08}");
                    }

                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Allwinner wty image is readed!");
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Allwinner wty image is invalid image!");
            }
        }
    }
}
