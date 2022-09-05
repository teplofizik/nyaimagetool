using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Info : ScriptStepGenerator
    {
        public Info() : base("info")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new InfoScriptStep();
        }

        public class InfoScriptStep : ScriptStep
        {
            public InfoScriptStep() : base("info")
            {

            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Kernel = Processor.GetKernel();
                if ((Kernel == null) || !Kernel.Loaded)
                    Log.Warning(0, "Kernel: not loaded");
                else
                    ImageFormat.Helper.LogHelper.KernelInfo(Kernel);

                var Fs = Processor.GetFs();
                if ((Fs == null) || !Fs.Loaded)
                    Log.Warning(0, "Filesystem: not loaded");
                else
                    ImageFormat.Helper.LogHelper.RamfsInfo(Fs);
                
                var Dtb = Processor.GetDevTree();
                if ((Dtb == null) || !Dtb.Loaded)
                    Log.Warning(0, "Device tree: not loaded");
                else
                    ImageFormat.Helper.LogHelper.DevtreeInfo(Dtb);

                return new ScriptStepResult(ScriptStepStatus.Ok, null);
            }
        }
    }
}
