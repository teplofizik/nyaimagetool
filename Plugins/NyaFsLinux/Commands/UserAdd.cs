using NyaFs.Filesystem.Universal.Identity;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Globalization;

namespace NyaFsLinux.Commands
{
    class UserAdd : ScriptStepGenerator
    {
        public UserAdd() : base("useradd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home")
            }));
            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("shell")
            }));
            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-G" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("groups"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid")
            }));
            AddConfig(new ScriptArgsConfig(4, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-G" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("groups"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home")
            }));
            AddConfig(new ScriptArgsConfig(5, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.EnumScriptArgsParam("option", new string[] { "-G" }),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("groups"),
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
            if (Args.ArgConfig >= 3)
            {
                var Name = A[2];
                var Uid = ParseId(A[3]);
                var Gid = ParseId(A[4]);
                var Home = (Args.ArgConfig >= 4) ? A[5] : "/home/" + Name;
                var Shell = (Args.ArgConfig >= 5) ? A[6] : "/bin/sh";
                return new UserAddScriptStep(Name, Uid, Gid, Home, Shell, SplitGroups(A[1]));
            }

            var UserName = A[0];
            var UserUid = ParseId(A[1]);
            var UserGid = ParseId(A[2]);
            var UserHome = (Args.ArgConfig >= 1) ? A[3] : "/home/" + UserName;
            var UserShell = (Args.ArgConfig >= 2) ? A[4] : "/bin/sh";
            return new UserAddScriptStep(UserName, UserUid, UserGid, UserHome, UserShell, new string[0]);
        }

        private static uint ParseId(string Value) =>
            UInt32.Parse(Value, NumberStyles.None, CultureInfo.InvariantCulture);

        private static string[] SplitGroups(string Value) =>
            String.IsNullOrEmpty(Value) ? new string[0] : Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        public class UserAddScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint Uid;
            private readonly uint Gid;
            private readonly string Home;
            private readonly string Shell;
            private readonly string[] Groups;

            public UserAddScriptStep(string Name, uint Uid, uint Gid, string Home, string Shell, string[] Groups) : base("useradd")
            {
                this.Name = Name;
                this.Uid = Uid;
                this.Gid = Gid;
                this.Home = Home;
                this.Shell = Shell;
                this.Groups = Groups;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                foreach (var Group in Groups)
                {
                    if (Db.FindGroup(Group) == null)
                        return new ScriptStepResult(ScriptStepStatus.Error, "Group '" + Group + "' not found");
                }

                if (!Db.AddUser(Name, Uid, Gid, "", Home, Shell, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                foreach (var Group in Groups)
                {
                    if (!Db.AddUserToGroup(Name, Group, out Error))
                        return new ScriptStepResult(ScriptStepStatus.Error, Error);
                }

                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                if (!Fs.Exists("/etc/shadow"))
                {
                    try
                    {
                        var Parent = Fs.GetParentDirectory("/etc/shadow");
                        if (Parent != null)
                            Parent.Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/shadow", 0, 0, 0x100, new byte[0]));
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                if (Fs.Exists("/etc/shadow"))
                    Linux.Users.UserLookup.AddShadowEntry(Fs, Name, "!");

                if (!Fs.Exists(Home))
                {
                    try
                    {
                        var Parent = Fs.GetParentDirectory(Home);
                        if (Parent != null)
                            Parent.Items.Add(new NyaFs.Filesystem.Universal.Items.Dir(Home, Uid, Gid, 493));
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, $"User {Name} ({Uid}:{Gid}) added!");
            }
        }
    }
}
