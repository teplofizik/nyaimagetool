using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Commands
{
    class Passwd : ScriptStepGenerator
    {

        public Passwd() : base("passwd")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("user"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("password")
            }));
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("user")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 1)
                return new PasswdScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
            else
                return new PasswdScriptStep(Args.RawArgs[0], "");
        }

        public class PasswdScriptStep : ScriptStep
        {
            private string User;
            private string Password;

            public PasswdScriptStep(string User, string Password) : base("passwd")
            {
                this.User = User;
                this.Password = Password;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();

                if(Fs == null)
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
                            if (Password == "")
                            {
                                NyaFs.Log.Ok(0, $"Password is correct");
                                return new ScriptStepResult(ScriptStepStatus.Ok, null);
                            }
                        }
                        else if (U.Hash != null)
                        {
                            if (U.CheckPassword(Password))
                                NyaFs.Log.Ok(0, $"Password is correct");
                            else
                                NyaFs.Log.Error(0, $"Password is incorrect");

                            return new ScriptStepResult(ScriptStepStatus.Ok, null);
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
