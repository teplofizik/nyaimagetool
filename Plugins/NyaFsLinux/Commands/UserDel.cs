using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;

namespace NyaFsLinux.Commands
{
    class UserDel : ScriptStepGenerator
    {
        public UserDel() : base("userdel")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) => new UserDelScriptStep(Args.RawArgs[0]);

        public class UserDelScriptStep : ScriptStep
        {
            private readonly string Name;

            public UserDelScriptStep(string Name) : base("userdel")
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
                if (!Db.RemoveUser(Name, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                Linux.Users.UserLookup.RemoveShadowEntry(Fs, Name);
                return new ScriptStepResult(ScriptStepStatus.Ok, $"User {Name} removed!");
            }
        }
    }
}
