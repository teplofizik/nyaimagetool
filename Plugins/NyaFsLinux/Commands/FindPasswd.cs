using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Commands
{
    class FindPasswd : ScriptStepGenerator
    {

        public FindPasswd() : base("findpasswd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("user"),
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new FindPasswdScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
        }

        public class FindPasswdScriptStep : ScriptStep
        {
            private string User;
            private string PasswordsFile;

            public FindPasswdScriptStep(string User, string PasswordsFile) : base("findpasswd")
            {
                this.User = User;
                this.PasswordsFile = PasswordsFile;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();

                if(!System.IO.File.Exists(PasswordsFile))
                    return new ScriptStepResult(ScriptStepStatus.Error, "Passwords file is not exists");

                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (!Fs.Exists("/etc/passwd"))
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem does not contains /etc/passwd file.");

                if (!Fs.Exists("/etc/shadow"))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Filesystem does not contains /etc/shadow file.");

                var PwLines = GetFileContent(Fs, "/etc/passwd");
                var ShLines = GetFileContent(Fs, "/etc/shadow");

                var Users = Linux.Users.UserParser.ParseUsers(PwLines, ShLines);

                foreach (var U in Users)
                {
                    if(U.Name == User)
                    {
                        if(U.NoPassword)
                            return new ScriptStepResult(ScriptStepStatus.Error, $"User '{User}' has no password.");
                        else if (U.Hash == "")
                        {
                            NyaFs.Log.Ok(0, $"Password for '{User}' is '' (empty string)");
                            return new ScriptStepResult(ScriptStepStatus.Ok, null);
                        }
                        else if (U.Hash != null)
                        {
                            var Passwords = System.IO.File.ReadAllLines(PasswordsFile);

                            foreach(var P in Passwords)
                            {
                                if(U.CheckPassword(P))
                                {
                                    NyaFs.Log.Ok(0, $"Password for '{User}' is '{P}'");
                                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                                }
                            }

                            return new ScriptStepResult(ScriptStepStatus.Error, "Correct password is not present at file '{PasswordsFile}'");
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Hash for user '{User}' is not loaded.");
                    }
                }

                return new ScriptStepResult(ScriptStepStatus.Error, $"User '{User}' is not found in /etc/passwd.");
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
