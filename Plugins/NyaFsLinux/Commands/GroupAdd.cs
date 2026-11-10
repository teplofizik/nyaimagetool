using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Globalization;

namespace NyaFsLinux.Commands
{
    class GroupAdd : ScriptStepGenerator
    {
        public GroupAdd() : base("groupadd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new GroupAddScriptStep(Args.RawArgs[0], UInt32.Parse(Args.RawArgs[1], NumberStyles.None, CultureInfo.InvariantCulture));

        public class GroupAddScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint Gid;

            public GroupAddScriptStep(string Name, uint Gid) : base("groupadd")
            {
                this.Name = Name;
                this.Gid = Gid;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                if (!Db.AddGroup(Name, Gid, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Group {Name} ({Gid}) added!");
            }
        }
    }
}
