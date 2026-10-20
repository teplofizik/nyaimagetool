using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsRemote.RemoteFs;
using NyaFsRemoteCore;
using System;

namespace NyaFsRemote.Commands
{
    internal sealed class RemotePut : ScriptStepGenerator
    {
        public RemotePut() : base("remoteput")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("protocol"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("host"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("login"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("pass"),
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("remotepath")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args) =>
            new Step(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2], Args.RawArgs[3], Args.RawArgs[4], Args.RawArgs[5]);

        private sealed class Step : ScriptStep
        {
            private readonly string Protocol;
            private readonly string Host;
            private readonly string Login;
            private readonly string Password;
            private readonly string FsPath;
            private readonly string RemotePath;

            public Step(string Protocol, string Host, string Login, string Password, string FsPath, string RemotePath)
                : base("remoteput")
            {
                this.Protocol = Protocol;
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

                if (string.Equals(Protocol, "ftp", StringComparison.OrdinalIgnoreCase))
                    return new ScriptStepResult(ScriptStepStatus.Error, "FTP is provided by NyaFsFtp: use ftpget/ftpput");

                try
                {
                    using (var Remote = RemoteFactory.Create(Protocol, Host, Login, Password))
                    {
                        Remote.Connect();
                        RemoteTransfer.Upload(Fs, FsPath, Remote, RemotePath);
                    }

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error,
                        $"remoteput {Protocol} {Host} {FsPath} -> {RemotePath}: {E.Message}");
                }
            }
        }
    }
}
