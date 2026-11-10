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
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;
            var Home = (Args.ArgConfig >= 1) ? A[3] : "/home/" + A[0];
            var Shell = (Args.ArgConfig >= 2) ? A[4] : "/bin/sh";

            return new UserAddScriptStep(A[0], ParseId(A[1]), ParseId(A[2]), Home, Shell);
        }

        private static uint ParseId(string Value) =>
            UInt32.Parse(Value, NumberStyles.None, CultureInfo.InvariantCulture);

        public class UserAddScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint Uid;
            private readonly uint Gid;
            private readonly string Home;
            private readonly string Shell;

            public UserAddScriptStep(string Name, uint Uid, uint Gid, string Home, string Shell) : base("useradd")
            {
                this.Name = Name;
                this.Uid = Uid;
                this.Gid = Gid;
                this.Home = Home;
                this.Shell = Shell;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                if (!Db.AddUser(Name, Uid, Gid, "", Home, Shell, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

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
