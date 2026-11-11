using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Globalization;

namespace NyaFsLinux.Commands
{
    class GroupMod : ScriptStepGenerator
    {
        public GroupMod() : base("groupmod")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new GroupModScriptStep(Args.RawArgs[0], UInt32.Parse(Args.RawArgs[1], NumberStyles.None, CultureInfo.InvariantCulture));

        public class GroupModScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint Gid;

            public GroupModScriptStep(string Name, uint Gid) : base("groupmod")
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
                if (!Db.UpdateGroup(Name, Gid, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Group {Name} changed to GID {Gid}!");
            }
        }
    }
}
