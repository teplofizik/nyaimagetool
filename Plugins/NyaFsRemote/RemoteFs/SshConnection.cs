using NyaFs.Remote;
using Renci.SshNet;
using System;
using System.Collections.Generic;

namespace NyaFsRemote.RemoteFs
{
    internal static class SshConnection
    {
        public static ConnectionInfo Create(string Host, int Port, RemoteAuth Auth)
        {
            if (Auth == null)
                throw new ArgumentNullException(nameof(Auth));

            if (String.IsNullOrEmpty(Auth.Login))
                throw new InvalidOperationException("SSH login is required");

            var Methods = new List<AuthenticationMethod>();

            if (Auth.HasPrivateKey)
            {
                var Key = String.IsNullOrEmpty(Auth.Passphrase)
                    ? new PrivateKeyFile(Auth.PrivateKeyPath)
                    : new PrivateKeyFile(Auth.PrivateKeyPath, Auth.Passphrase);

                Methods.Add(new PrivateKeyAuthenticationMethod(Auth.Login, Key));
            }

            if (Auth.HasPassword)
                Methods.Add(new PasswordAuthenticationMethod(Auth.Login, Auth.Password));

            if (Methods.Count == 0)
                throw new InvalidOperationException(
                    $"No SSH credentials for '{Auth.Login}': specify a password or a private key");

            var Info = new ConnectionInfo(Host, Port, Auth.Login, Methods.ToArray());
            Info.Timeout = TimeSpan.FromSeconds(30);
            return Info;
        }
    }
}
