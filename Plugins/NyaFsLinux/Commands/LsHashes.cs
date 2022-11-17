using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Commands
{
    class LsHashes : ScriptStepGenerator
    {

        public LsHashes() : base("lshashes")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LsHashesScriptStep();
        }


        public class LsHashesScriptStep : ScriptStep
        {
            public LsHashesScriptStep() : base("lshashes")
            {

            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();

                if(Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (!Fs.Exists("/etc/passwd"))
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem does not contains /etc/passwd file.");

                if (!Fs.Exists("/etc/shadow"))
                    NyaFs.Log.Warning(0, "Filesystem does not contains /etc/shadow file.");

                var PwLines = GetFileContent(Fs, "/etc/passwd");
                var ShLines = GetFileContent(Fs, "/etc/shadow");

                var Users = Linux.Users.UserParser.ParseUsers(PwLines, ShLines);

                NyaFs.Log.Write(0, $"{"Name".PadRight(12)} {"Type".PadRight(8)} {"Salt".PadRight(18)} Hash");
                foreach (var U in Users)
                {
                    var UidGid = $"{U.UID}:{U.GID}";

                    if((U.Hash == ""))
                        NyaFs.Log.Write(0, $"{U.Name.PadRight(12)} {"no".PadRight(8)} {"-".PadRight(18)} {"no password".PadRight(25)}");
                    else if ((U.Hash != null) && !U.NoPassword)
                        NyaFs.Log.Write(0, $"{U.Name.PadRight(12)} {U.HashType.PadRight(8)} {U.Salt.PadRight(18)} {U.HashValue.PadRight(25)}");
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, null);
            }

            private string[] GetFileContent(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path)
            {
                if (Fs.Exists(Path))
                {
                    var File = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
                    var Text = UTF8Encoding.UTF8.GetString(File.Content);

                    return Text.Split(new char[] { '\n' });
                }
                else
                    return null;
            }
        }
    }
}
