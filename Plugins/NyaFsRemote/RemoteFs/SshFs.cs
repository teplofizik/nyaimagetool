using NyaFsRemoteCore;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace NyaFsRemote.RemoteFs
{
    /// <summary>
    /// SSH exec channel transport (no SFTP/SCP required): metadata via stat/ls/readlink,
    /// file contents via "cat" with stdio streams.
    /// </summary>
    internal class SshFs : IRemoteFs
    {
        protected readonly string Host;
        protected readonly int Port;
        protected readonly string User;
        protected readonly string Password;

        protected SshClient Client;

        public SshFs(string Host, int Port, string User, string Password)
        {
            this.Host = Host;
            this.Port = Port;
            this.User = User;
            this.Password = Password;
        }

        protected ConnectionInfo CreateConnectionInfo()
        {
            var Info = new ConnectionInfo(Host, Port, User,
                new PasswordAuthenticationMethod(User, Password));
            Info.Timeout = TimeSpan.FromSeconds(30);
            return Info;
        }

        public virtual void Connect()
        {
            Client = new SshClient(CreateConnectionInfo());
            Client.KeepAliveInterval = TimeSpan.FromSeconds(15);
            Client.Connect();
        }

        protected (int ExitCode, string Output) Run(string Command)
        {
            using (var Cmd = Client.CreateCommand(Command))
            {
                Cmd.CommandTimeout = TimeSpan.FromMinutes(30);
                var Output = Cmd.Execute();
                return (Cmd.ExitStatus ?? -1, Output);
            }
        }

        private static string Combine(string Path, string Name) =>
            (Path.EndsWith("/") ? Path : Path + "/") + Name;

        public RemoteEntry GetEntry(string Path)
        {
            var Stat = Run($"stat -c '%f|%u|%g|%s' -- {ShellQuote.Quote(Path)}");
            uint RawMode = 0;

            if (Stat.ExitCode != 0)
            {
                Stat = Run($"stat -c '%a|%u|%g|%s' -- {ShellQuote.Quote(Path)}");
                if (Stat.ExitCode != 0)
                    return null;

                var Octal = Stat.Output.Trim().Split('|');
                if (Octal.Length != 4)
                    return null;

                try { RawMode = Convert.ToUInt32(Octal[0], 8); }
                catch { return null; }

                if (Run($"test -d -- {ShellQuote.Quote(Path)}").ExitCode == 0) RawMode |= 0x4000;
                else if (Run($"test -L -- {ShellQuote.Quote(Path)}").ExitCode == 0) RawMode |= 0xA000;
                else if (Run($"test -f -- {ShellQuote.Quote(Path)}").ExitCode == 0) RawMode |= 0x8000;
            }

            var Parts = Stat.Output.Trim().Split('|');
            if (Parts.Length != 4)
                return null;

            if (RawMode == 0)
            {
                if (!UInt32.TryParse(Parts[0], System.Globalization.NumberStyles.HexNumber, null, out RawMode))
                    return null;
            }

            uint User = UInt32.TryParse(Parts[1], out uint U) ? U : 0;
            uint Group = UInt32.TryParse(Parts[2], out uint G) ? G : 0;
            long Size = Int64.TryParse(Parts[3], out long S) ? S : 0;

            uint Type = RawMode & 0xF000;
            var Entry = new RemoteEntry
            {
                Name = System.IO.Path.GetFileName(Path.TrimEnd('/')),
                Path = Path,
                IsDirectory = Type == 0x4000,
                IsFile = Type == 0x8000,
                IsSymLink = Type == 0xA000,
                Mode = RawMode & 0xFFF,
                User = User,
                Group = Group,
                Size = Size
            };

            if (Entry.IsSymLink)
            {
                var Link = Run($"readlink -- {ShellQuote.Quote(Path)}");
                if (Link.ExitCode == 0)
                    Entry.LinkTarget = Link.Output.Trim();
            }

            return Entry;
        }

        public RemoteEntry[] List(string Path)
        {
            var Result = Run($"ls -1A -- {ShellQuote.Quote(Path)}");
            if (Result.ExitCode != 0)
                throw new IOException($"Cannot list remote directory '{Path}'");

            var Res = new List<RemoteEntry>();
            foreach (var Name in Result.Output.Split('\n'))
            {
                var N = Name.TrimEnd('\r');
                if (N.Length == 0)
                    continue;

                var Entry = GetEntry(Combine(Path, N));
                if (Entry != null)
                    Res.Add(Entry);
            }

            return Res.ToArray();
        }

        public virtual byte[] ReadFile(string Path)
        {
            using (var Cmd = Client.CreateCommand($"cat -- {ShellQuote.Quote(Path)}"))
            {
                Cmd.CommandTimeout = TimeSpan.FromMinutes(30);
                var Task = Cmd.ExecuteAsync(CancellationToken.None);
                using (var Output = new MemoryStream())
                {
                    Cmd.OutputStream.CopyTo(Output);
                    Task.GetAwaiter().GetResult();

                    if ((Cmd.ExitStatus ?? -1) != 0)
                        throw new IOException($"Cannot read remote file '{Path}'");

                    return Output.ToArray();
                }
            }
        }

        public virtual void WriteFile(string Path, byte[] Data)
        {
            using (var Cmd = Client.CreateCommand($"cat > {ShellQuote.Quote(Path)}"))
            {
                Cmd.CommandTimeout = TimeSpan.FromMinutes(30);
                var Task = Cmd.ExecuteAsync(CancellationToken.None);

                using (var Input = Cmd.CreateInputStream())
                    Input.Write(Data, 0, Data.Length);

                Task.GetAwaiter().GetResult();

                if ((Cmd.ExitStatus ?? -1) != 0)
                    throw new IOException($"Cannot write remote file '{Path}'");
            }
        }

        public void CreateDirectory(string Path)
        {
            var Result = Run($"mkdir -p -- {ShellQuote.Quote(Path)}");
            if (Result.ExitCode != 0)
                throw new IOException($"Cannot create remote directory '{Path}'");
        }

        public virtual void Dispose()
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
