using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;

namespace NyaFsLinux.Commands
{
    class GroupDel : ScriptStepGenerator
    {
        public GroupDel() : base("groupdel")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) => new GroupDelScriptStep(Args.RawArgs[0]);

        public class GroupDelScriptStep : ScriptStep
        {
            private readonly string Name;

            public GroupDelScriptStep(string Name) : base("groupdel")
            {
                this.Name = Name;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                if (!Db.RemoveGroup(Name, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Group {Name} removed!");
            }
        }
    }
}
