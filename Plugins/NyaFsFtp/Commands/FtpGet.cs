using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsRemoteCore;
using System;

namespace NyaFsFtp.Commands
{
    internal sealed class FtpGet : ScriptStepGenerator
    {
        public FtpGet() : base("ftpget")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("host"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("login"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("pass"),
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("remotepath")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new Step(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2], Args.RawArgs[3], Args.RawArgs[4]);

        private sealed class Step : ScriptStep
        {
            private readonly string Host;
            private readonly string Login;
            private readonly string Password;
            private readonly string FsPath;
            private readonly string RemotePath;

            public Step(string Host, string Login, string Password, string FsPath, string RemotePath)
                : base("ftpget")
            {
                this.Host = Host;
                this.Login = Login;
                this.Password = Password;
                this.FsPath = FsPath;
                this.RemotePath = RemotePath;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                try
                {
                    var (RemoteHost, Port) = RemoteUri.SplitHostPort(Host, 21);
                    using (var Remote = new FtpFs(RemoteHost, Port, Login, Password))
                    {
                        Remote.Connect();
                        RemoteTransfer.Download(Remote, RemotePath, Fs, FsPath);
                    }

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error,
                        $"ftpget {Host} {RemotePath} -> {FsPath}: {E.Message}");
                }
            }
        }
    }
}
