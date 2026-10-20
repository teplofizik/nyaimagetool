using NyaFs.Remote;
using System;

namespace NyaFsRemote.RemoteFs
{
    internal static class RemoteFactory
    {
        public static IRemoteFs Create(string Protocol, string HostPort, string Login, string Password) =>
            Create(Protocol, HostPort, RemoteAuth.FromPassword(Login, Password));

        public static IRemoteFs Create(string Protocol, string HostPort, RemoteAuth Auth)
        {
            var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 22);

            switch ((Protocol ?? "").ToLowerInvariant())
            {
                case "ssh": return new SshFs(Host, Port, Auth);
                case "scp": return new ScpFs(Host, Port, Auth);
                case "sftp": return new SftpFs(Host, Port, Auth);
                default: throw new ArgumentException($"Unsupported remote protocol '{Protocol}'");
            }
        }
    }
}
