using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Globalization;

namespace NyaFsLinux.Commands
{
    class Usermod : ScriptStepGenerator
    {
        public Usermod() : base("usermod")
        {
            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-G" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("groups"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-aG" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("groups"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
            AddConfig(new ScriptArgsConfig(4, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-L" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
            AddConfig(new ScriptArgsConfig(5, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-U" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name")
            }));
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("shell")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("shell")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;
            if ((Args.ArgConfig == 4) || (Args.ArgConfig == 5))
                return new LockScriptStep(A[1], Args.ArgConfig == 4);

            if ((Args.ArgConfig == 2) || (Args.ArgConfig == 3))
                return new UsermodScriptStep(A[2], null, null, null, null, SplitGroups(A[1]), Args.ArgConfig == 3);

            if (Args.ArgConfig == 0)
                return new UsermodScriptStep(A[0], null, null, A[1], A[2], null, false);

            return new UsermodScriptStep(A[0],
                UInt32.Parse(A[1], NumberStyles.None, CultureInfo.InvariantCulture),
                UInt32.Parse(A[2], NumberStyles.None, CultureInfo.InvariantCulture),
                A[3], A[4], null, false);
        }

        private static string[] SplitGroups(string Value) =>
            String.IsNullOrEmpty(Value) ? new string[0] : Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        public class LockScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly bool Lock;

            public LockScriptStep(string Name, bool Lock) : base("usermod")
            {
                this.Name = Name;
                this.Lock = Lock;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                string Error;
                if (!Linux.Users.UserLookup.SetLock(Fs, Name, Lock, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, "User " + Name + (Lock ? " locked!" : " unlocked!"));
            }
        }
        public class UsermodScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint? Uid;
            private readonly uint? Gid;
            private readonly string Home;
            private readonly string Shell;
            private readonly string[] Groups;
            private readonly bool AppendGroups;

            public UsermodScriptStep(string Name, uint? Uid, uint? Gid, string Home, string Shell,
                string[] Groups, bool AppendGroups) : base("usermod")
            {
                this.Name = Name;
                this.Uid = Uid;
                this.Gid = Gid;
                this.Home = Home;
                this.Shell = Shell;
                this.Groups = Groups;
                this.AppendGroups = AppendGroups;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;

                if (Groups != null)
                {
                    if (!Db.SetUserGroups(Name, Groups, AppendGroups, out Error))
                        return new ScriptStepResult(ScriptStepStatus.Error, Error);
                }
                else if (!Db.UpdateUser(Name, Uid, Gid, Home, Shell, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"User {Name} updated!");
            }
        }
    }
}
