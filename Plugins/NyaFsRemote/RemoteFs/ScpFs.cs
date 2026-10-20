using Renci.SshNet;
using System;
using System.IO;

namespace NyaFsRemote.RemoteFs
{
    /// <summary>
    /// SCP transport. Metadata/listing uses an SSH exec channel; file contents use SCP.
    /// </summary>
    internal sealed class ScpFs : SshFs
    {
        private ScpClient Scp;

        public ScpFs(string Host, int Port, string User, string Password)
            : base(Host, Port, User, Password)
        {
        }

        public override void Connect()
        {
            base.Connect();

            Scp = new ScpClient(CreateConnectionInfo(), RemotePathTransformation.ShellQuote);
            Scp.KeepAliveInterval = TimeSpan.FromSeconds(15);
            Scp.Connect();
        }

        public override byte[] ReadFile(string Path)
        {
            using (var Output = new MemoryStream())
            {
                Scp.Download(Path, Output);
                return Output.ToArray();
            }
        }

        public override void WriteFile(string Path, byte[] Data)
        {
            using (var Input = new MemoryStream(Data))
                Scp.Upload(Input, Path);
        }

        public override void Dispose()
        {
            if (Scp != null)
            {
                try { Scp.Disconnect(); } catch { }
                Scp.Dispose();
                Scp = null;
            }

            base.Dispose();
        }
    }
}
