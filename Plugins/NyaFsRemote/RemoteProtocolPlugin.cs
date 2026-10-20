using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.Remote;
using NyaFsRemote.RemoteFs;
using System;

namespace NyaFsRemote
{
    /// <summary>SSH/SCP/SFTP protocol provider for the core remoteget/remoteput commands.</summary>
    public sealed class RemoteProtocolPlugin : RemoteFilesystemPlugin
    {
        public RemoteProtocolPlugin() : base("remote-protocols")
        {
        }

        public override string[] Protocols => new[] { "ssh", "scp", "sftp" };

        public override IRemoteFs Create(string Protocol, string HostPort, string Login, string Password) =>
            Create(Protocol, HostPort, RemoteAuth.FromPassword(Login, Password));

        public override IRemoteFs Create(string Protocol, string HostPort, RemoteAuth Auth)
        {
            var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 22);

            switch ((Protocol ?? "").ToLowerInvariant())
            {
                case "ssh": return new SshFs(Host, Port, Auth);
                case "scp": return new ScpFs(Host, Port, Auth);
                case "sftp": return new SftpFs(Host, Port, Auth);
                default: throw new ArgumentException($"Unsupported SSH protocol '{Protocol}'");
            }
        }
    }
}
