using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.Remote;
using System;

namespace NyaFsFtp
{
    public sealed class FtpProtocolPlugin : RemoteFilesystemPlugin
    {
        public FtpProtocolPlugin() : base("ftp-protocols")
        {
        }

        public override string[] Protocols => new[] { "ftp" };

        public override IRemoteFs Create(string Protocol, string HostPort, string Login, string Password) =>
            Create(Protocol, HostPort, RemoteAuth.FromPassword(Login, Password));

        public override IRemoteFs Create(string Protocol, string HostPort, RemoteAuth Auth)
        {
            var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 21);
            return new FtpFs(Host, Port, Auth);
        }
    }
}
