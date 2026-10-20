#pragma warning disable SYSLIB0014
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using NyaFsRemoteCore;

namespace NyaFsFtp
{
    internal sealed class FtpFs : IRemoteFs
    {
        private readonly string Host;
        private readonly int Port;
        private readonly string User;
        private readonly string Password;

        public FtpFs(string Host, int Port, string User, string Password)
        {
            this.Host = Host;
            this.Port = Port;
            this.User = User;
            this.Password = Password;
        }

        public void Connect()
        {
        }

        private string Url(string Path)
        {
            var Parts = new List<string>();
            foreach (var Part in (Path ?? "").Replace('\\', '/').Split('/'))
            {
                if (Part.Length == 0)
                    continue;

                Parts.Add(Uri.EscapeDataString(Part));
            }

            return $"ftp://{Host}:{Port}/" + string.Join("/", Parts);
        }

        private FtpWebRequest Request(string Path, string Method)
        {
            var Req = (FtpWebRequest)WebRequest.Create(Url(Path));
            Req.Method = Method;
            Req.Credentials = new NetworkCredential(User, Password);
            Req.UseBinary = true;
            Req.UsePassive = true;
            Req.KeepAlive = false;
            Req.Timeout = 30000;
            return Req;
        }

        private static string Parent(string Path)
        {
            var P = Path.TrimEnd('/');
            int Slash = P.LastIndexOf('/');
            return (Slash <= 0) ? "/" : P.Substring(0, Slash);
        }

        private static string BaseName(string Path)
        {
            var P = Path.TrimEnd('/');
            int Slash = P.LastIndexOf('/');
            return (Slash < 0) ? P : P.Substring(Slash + 1);
        }

        private static uint Permissions(string Value)
        {
            if (string.IsNullOrEmpty(Value) || (Value.Length < 10))
                return 0;

            uint Res = 0;
            uint[] Bits = { 0x100, 0x080, 0x040, 0x020, 0x010, 0x008, 0x004, 0x002, 0x001 };

            for (int i = 0; i < 9; i++)
            {
                char C = Value[i + 1];
                if (C != '-')
                    Res |= Bits[i];
            }

            return Res;
        }

        private static RemoteEntry ParseUnix(string Line)
        {
            var Parts = Line.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            if (Parts.Length < 9)
                return null;

            char Type = Parts[0].Length > 0 ? Parts[0][0] : '?';
            var Entry = new RemoteEntry
            {
                Name = Parts[8],
                Path = Parts[8],
                IsDirectory = Type == 'd',
                IsFile = Type == '-' || Type == 'l',
                IsSymLink = Type == 'l',
                Mode = Permissions(Parts[0]),
                Size = Int64.TryParse(Parts[4], out long S) ? S : 0
            };

            if (Entry.IsSymLink)
            {
                int Arrow = Entry.Name.IndexOf(" -> ", StringComparison.Ordinal);
                if (Arrow > 0)
                {
                    Entry.LinkTarget = Entry.Name.Substring(Arrow + 4);
                    Entry.Name = Entry.Name.Substring(0, Arrow);
                }
            }

            if (Entry.IsDirectory)
                Entry.Mode = 0x1ED;
            else if (Entry.Mode == 0)
                Entry.Mode = 0x1A4;

            return Entry;
        }

        private static RemoteEntry ParseDos(string Line)
        {
            var Parts = Line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (Parts.Length < 3)
                return null;

            int Dir = Line.IndexOf("<DIR>", StringComparison.OrdinalIgnoreCase);
            long Size = 0;
            if (Dir < 0)
                Int64.TryParse(Parts[2], out Size);

            var Entry = new RemoteEntry
            {
                Name = Parts[Parts.Length - 1],
                Path = Parts[Parts.Length - 1],
                IsDirectory = Dir >= 0,
                IsFile = Dir < 0,
                Mode = Dir >= 0 ? 0x1EDu : 0x1A4u,
                Size = Size
            };

            return Entry;
        }

        public RemoteEntry[] List(string Path)
        {
            using (var Response = (FtpWebResponse)Request(Path, WebRequestMethods.Ftp.ListDirectoryDetails).GetResponse())
            using (var Reader = new StreamReader(Response.GetResponseStream()))
            {
                var Res = new List<RemoteEntry>();
                string Line;
                while ((Line = Reader.ReadLine()) != null)
                {
                    var Name = Line.Trim();
                    if (Name.Length == 0)
                        continue;

                    var Entry = ParseUnix(Name) ?? ParseDos(Name);
                    if (Entry == null)
                        continue;

                    Entry.Path = (Path.EndsWith("/") ? Path : Path + "/") + Entry.Name;
                    Res.Add(Entry);
                }

                return Res.ToArray();
            }
        }

        public RemoteEntry GetEntry(string Path)
        {
            if ((Path == "/") || (Path.Length == 0))
                return new RemoteEntry { Name = "", Path = "/", IsDirectory = true, Mode = 0x1ED };

            var Name = BaseName(Path);
            foreach (var Entry in List(Parent(Path)))
            {
                if (Entry.Name == Name)
                    return Entry;
            }

            return null;
        }

        public byte[] ReadFile(string Path)
        {
            using (var Response = (FtpWebResponse)Request(Path, WebRequestMethods.Ftp.DownloadFile).GetResponse())
            using (var Input = Response.GetResponseStream())
            using (var Output = new MemoryStream())
            {
                Input.CopyTo(Output);
                return Output.ToArray();
            }
        }

        public void WriteFile(string Path, byte[] Data)
        {
            var Req = Request(Path, WebRequestMethods.Ftp.UploadFile);
            using (var Output = Req.GetRequestStream())
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
                    using (var Response = (FtpWebResponse)Request(Current, WebRequestMethods.Ftp.MakeDirectory).GetResponse())
                    {
                    }
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
#pragma warning restore SYSLIB0014
