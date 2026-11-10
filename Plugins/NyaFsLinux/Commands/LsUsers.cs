using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Commands
{
    class LsUsers : ScriptStepGenerator
    {
        public LsUsers() : base("lsusr")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args) => new LsUsersScriptStep();

        public class LsUsersScriptStep : ScriptStep
        {
            public LsUsersScriptStep() : base("lsusr") { }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (!Fs.Exists("/etc/passwd"))
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem does not contains /etc/passwd file.");

                if (!Fs.Exists(Linux.Users.UserLookup.ShadowPath))
                    NyaFs.Log.Warning(0, "Filesystem does not contains /etc/shadow file.");

                var Users = Linux.Users.UserLookup.Load(Fs);

                NyaFs.Log.Write(0, $"{"Name".PadRight(12)} {"UID:GID".PadRight(14)} {"Home".PadRight(25)} {"Shell".PadRight(16)}   Password");
                foreach (var U in Users)
                {
                    var UidGid = $"{U.UID}:{U.GID}";
                    var Info = $"{U.Name.PadRight(12)} {UidGid.PadRight(14)} {U.Home.PadRight(25)} {U.Shell.PadRight(16)}   ";

                    if (U.Hash != null)
                    {
                        if (U.NoPassword)
                            NyaFs.Log.Ok(0, Info + "no password enabled");
                        else if (U.Hash == "")
                            NyaFs.Log.Warning(0, Info + "no password (unrestricted access)");
                        else
                            NyaFs.Log.Ok(0, Info + $"hash {U.HashType} with salt '{U.Salt}'");
                    }
                    else
                        NyaFs.Log.Warning(0, Info + "no hash info!");
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, null);
            }
        }
    }
}
