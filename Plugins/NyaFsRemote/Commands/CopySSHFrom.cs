using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsRemote.RemoteFs;
using NyaFsRemoteCore;
using System;

namespace NyaFsRemote.Commands
{
    /// <summary>
    /// Backward-compatible "copysshfrom" command. Prefer "remoteget ssh ...".
    /// </summary>
    internal sealed class CopySSHFrom : ScriptStepGenerator
    {
        public CopySSHFrom() : base("copysshfrom")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("ipport"),
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
                : base("copysshfrom")
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
                    var (RemoteHost, Port) = RemoteUri.SplitHostPort(Host, 22);
                    using (var Remote = new SshFs(RemoteHost, Port, Login, Password))
                    {
                        Remote.Connect();
                        RemoteTransfer.Download(Remote, RemotePath, Fs, FsPath);
                    }

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error,
                        $"copysshfrom {Host} {RemotePath} -> {FsPath}: {E.Message}");
                }
            }
        }
    }
}
