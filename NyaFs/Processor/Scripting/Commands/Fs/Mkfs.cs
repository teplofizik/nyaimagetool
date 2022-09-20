using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Mkfs : ScriptStepGenerator
    {
        public Mkfs() : base("mkfs")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {}));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new MkfsScriptStep();
        }

        public class MkfsScriptStep : ScriptStep
        {
            public MkfsScriptStep() : base("mkfs")
            {

            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
                Processor.SetFs(Fs);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Created empty filesystem!");
            }
        }
    }
}
