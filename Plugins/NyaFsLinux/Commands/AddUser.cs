using NyaFs.Filesystem.Universal.Identity;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Globalization;

namespace NyaFsLinux.Commands
{
    class AddUser : ScriptStepGenerator
    {
        public AddUser() : base("adduser")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid")
            }));
            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("name"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("uid"),
                new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("gid"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("home")
            }));
            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[]
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
            var Uid = ParseId(A[1]);
            var Gid = (Args.ArgConfig >= 1) ? ParseId(A[2]) : Uid;
            var Home = (Args.ArgConfig >= 2) ? A[3] : "/home/" + A[0];
            var Shell = (Args.ArgConfig >= 3) ? A[4] : "/bin/sh";
            return new AddUserScriptStep(A[0], Uid, Gid, Home, Shell, Args.ArgConfig == 0);
        }

        private static uint ParseId(string Value) =>
            UInt32.Parse(Value, NumberStyles.None, CultureInfo.InvariantCulture);

        public class AddUserScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint Uid;
            private readonly uint Gid;
            private readonly string Home;
            private readonly string Shell;
            private readonly bool CreateGroup;

            public AddUserScriptStep(string Name, uint Uid, uint Gid, string Home, string Shell, bool CreateGroup) : base("adduser")
            {
                this.Name = Name;
                this.Uid = Uid;
                this.Gid = Gid;
                this.Home = Home;
                this.Shell = Shell;
                this.CreateGroup = CreateGroup;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var Db = Fs.Identities;
                string Error;
                if (CreateGroup && (Db.FindGroup(Gid) == null))
                {
                    if (!Db.AddGroup(Name, Gid, out Error))
                        return new ScriptStepResult(ScriptStepStatus.Error, Error);
                }

                if (!Db.AddUser(Name, Uid, Gid, "", Home, Shell, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
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
