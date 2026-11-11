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
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-n" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("new-name"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;
            if (Args.ArgConfig == 1)
                return new RenameGroupScriptStep(A[2], A[1]);

            return new GroupModScriptStep(A[0], UInt32.Parse(A[1], NumberStyles.None, CultureInfo.InvariantCulture));
        }

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

        public class RenameGroupScriptStep : ScriptStep
        {
            private readonly string OldName;
            private readonly string NewName;

            public RenameGroupScriptStep(string OldName, string NewName) : base("groupmod")
            {
                this.OldName = OldName;
                this.NewName = NewName;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                if (!Db.RenameGroup(OldName, NewName, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Group {OldName} renamed to {NewName}!");
            }
        }
    }
}
