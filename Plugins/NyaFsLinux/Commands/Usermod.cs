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
            if (Args.ArgConfig == 0)
                return new UsermodScriptStep(A[0], null, null, A[1], A[2]);

            return new UsermodScriptStep(A[0],
                UInt32.Parse(A[1], NumberStyles.None, CultureInfo.InvariantCulture),
                UInt32.Parse(A[2], NumberStyles.None, CultureInfo.InvariantCulture),
                A[3], A[4]);
        }

        public class UsermodScriptStep : ScriptStep
        {
            private readonly string Name;
            private readonly uint? Uid;
            private readonly uint? Gid;
            private readonly string Home;
            private readonly string Shell;

            public UsermodScriptStep(string Name, uint? Uid, uint? Gid, string Home, string Shell) : base("usermod")
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
                if (!Db.UpdateUser(Name, Uid, Gid, Home, Shell, out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);
                if (!Db.Save(out Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                return new ScriptStepResult(ScriptStepStatus.Ok, $"User {Name} updated!");
            }
        }
    }
}
