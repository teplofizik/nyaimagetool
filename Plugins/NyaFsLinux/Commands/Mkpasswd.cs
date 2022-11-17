using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Commands
{
    class Mkpasswd : ScriptStepGenerator
    {

        public Mkpasswd() : base("mkpasswd")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("user"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("password")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new MkpasswdScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
        }

        public class MkpasswdScriptStep : ScriptStep
        {
            private string User;
            private string Password;

            public MkpasswdScriptStep(string User, string Password) : base("mkpasswd")
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
                        if (U.Hash != null)
                        {
                            var TSalt = Linux.ManagedUnixCrypt.GenerateSalt();
                            var NewHash = Linux.ManagedUnixCrypt.Crypt(Password, TSalt);

                            var Element = Fs.GetElement("/etc/shadow");
                            var File = Element as NyaFs.Filesystem.Universal.Items.File;

                            File.Content = UpdateHashInShadow(ShLines, User, U.Hash, NewHash);

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

            private byte[] UpdateHashInShadow(string[] Original, string User, string OldHash, string NewHash)
            {
                var Res = new string[Original.Length];
                for(int i = 0; i < Original.Length; i++)
                {
                    var O = Original[i];
                    if (O.StartsWith($"{User}:"))
                        O = O.Replace(OldHash, NewHash);

                    Res[i] = O;
                }

                var Str = String.Join("\n", Res);
                return UTF8Encoding.UTF8.GetBytes(Str);
            }
        }
    }
}
