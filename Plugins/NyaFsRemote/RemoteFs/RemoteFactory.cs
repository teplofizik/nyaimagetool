using NyaFsRemoteCore;
using System;

namespace NyaFsRemote.RemoteFs
{
    internal static class RemoteFactory
    {
        public static IRemoteFs Create(string Protocol, string HostPort, string Login, string Password)
        {
            switch ((Protocol ?? "").ToLowerInvariant())
            {
                case "ssh":
                    {
                        var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 22);
                        return new SshFs(Host, Port, Login, Password);
                    }

                case "scp":
                    {
                        var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 22);
                        return new ScpFs(Host, Port, Login, Password);
                    }

                case "sftp":
                    {
                        var (Host, Port) = RemoteUri.SplitHostPort(HostPort, 22);
                        return new SftpFs(Host, Port, Login, Password);
                    }

                default:
                    throw new ArgumentException($"Unsupported remote protocol '{Protocol}'");
            }
        }
    }
}
