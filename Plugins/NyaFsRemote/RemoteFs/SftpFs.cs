using NyaFs.Remote;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsRemote.RemoteFs
{
    internal sealed class SftpFs : IRemoteFs
    {
        private readonly string Host;
        private readonly int Port;
        private readonly RemoteAuth Auth;

        private SftpClient Client;

        public SftpFs(string Host, int Port, RemoteAuth Auth)
        {
            this.Host = Host;
            this.Port = Port;
            this.Auth = Auth;
        }

        private ConnectionInfo CreateConnectionInfo() => SshConnection.Create(Host, Port, Auth);

        public void Connect()
        {
            Client = new SftpClient(CreateConnectionInfo());
            Client.KeepAliveInterval = TimeSpan.FromSeconds(15);
            Client.Connect();
        }

        private static uint Mode(SftpFileAttributes A)
        {
            uint Res = 0;

            if (A.OwnerCanRead) Res |= 0x100;
            if (A.OwnerCanWrite) Res |= 0x080;
            if (A.OwnerCanExecute) Res |= 0x040;
            if (A.GroupCanRead) Res |= 0x020;
            if (A.GroupCanWrite) Res |= 0x010;
            if (A.GroupCanExecute) Res |= 0x008;
            if (A.OthersCanRead) Res |= 0x004;
            if (A.OthersCanWrite) Res |= 0x002;
            if (A.OthersCanExecute) Res |= 0x001;
            if (A.IsUIDBitSet) Res |= 0x800;
            if (A.IsGroupIDBitSet) Res |= 0x400;
            if (A.IsStickyBitSet) Res |= 0x200;

            return Res;
        }

        private RemoteEntry ToEntry(string Path, SftpFileAttributes A)
        {
            return new RemoteEntry
            {
                Name = System.IO.Path.GetFileName(Path.TrimEnd('/')),
                Path = Path,
                IsDirectory = A.IsDirectory,
                IsFile = !A.IsDirectory && !A.IsSymbolicLink,
                IsSymLink = A.IsSymbolicLink,
                Mode = Mode(A),
                User = Convert.ToUInt32(A.UserId),
                Group = Convert.ToUInt32(A.GroupId),
                Size = A.Size
            };
        }

        public RemoteEntry GetEntry(string Path)
        {
            try
            {
                return ToEntry(Path, Client.GetAttributes(Path));
            }
            catch (SftpPathNotFoundException)
            {
                return null;
            }
        }

        public RemoteEntry[] List(string Path)
        {
            var Res = new List<RemoteEntry>();
            foreach (var File in Client.ListDirectory(Path))
            {
                if ((File.Name == ".") || (File.Name == ".."))
                    continue;

                Res.Add(ToEntry(File.FullName, File.Attributes));
            }

            return Res.ToArray();
        }

        public byte[] ReadFile(string Path)
        {
            using (var Input = Client.OpenRead(Path))
            using (var Output = new MemoryStream())
            {
                Input.CopyTo(Output);
                return Output.ToArray();
            }
        }

        public void WriteFile(string Path, byte[] Data)
        {
            using (var Output = Client.OpenWrite(Path))
                Output.Write(Data, 0, Data.Length);
        }

        public void CreateDirectory(string Path)
        {
            var Current = "";
            foreach (var Part in Path.Trim('/').Split('/'))
            {
                if (Part.Length == 0)
                    continue;

                Current += "/" + Part;

                try
                {
                    if (!Client.Exists(Current))
                        Client.CreateDirectory(Current);
                }
                catch (SshException)
                {
                }
            }
        }

        public void Dispose()
        {
            if (Client != null)
            {
                try { Client.Disconnect(); } catch { }
                Client.Dispose();
                Client = null;
            }
        }
    }
}
