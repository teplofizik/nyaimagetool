using NyaFs.Processor;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaFsRemote.RemoteFs
{
    internal class SshFs
    {
        SshClient client;
        long Timeout = 10000;

        public SshFs(string Host, string User, string Password)
        {
            client = new SshClient(Host, User, Password);
        }

        public bool Connect()
        {
            try
            {
                client.Connect();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            client.Disconnect();
        }

        public bool IsDir(string Path)
        {
            SshCommand sc = client.CreateCommand($"test -d {Path}");
            sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return true;
            }
            return false;
        }

        public bool IsFile(string Path)
        {
            SshCommand sc = client.CreateCommand($"test -f {Path}");
            sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return true;
            }
            return false;
        }


        public string[] GetDirContent(string Path)
        {
            SshCommand sc = client.CreateCommand($"ls {Path}");
            var Res = sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return Res.Split('\n').Where(P => P.Length > 0).ToArray();
            }
            return null;
        }

        public NyaFs.Filesystem.Universal.Items.File ReadFile(string Path)
        {
            var Content = GetFileContent(Path);

            if (Content != null)
            {
                uint User = GetUserId(Path);
                uint Group = GetGroupId(Path);
                uint Mode = GetAccessRights(Path);

                return new NyaFs.Filesystem.Universal.Items.File(Path, User, Group, Mode, Content);
            }
            else
                return null;
        }

        public uint GetUserId(string Path)
        {
            SshCommand sc = client.CreateCommand($"stat -c %u {Path}");
            var Res = sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return Convert.ToUInt32(Res);
            }
            return 0;
        }

        public string GetFileType(string Path)
        {
            SshCommand sc = client.CreateCommand($"stat -c %A {Path}");
            var Res = sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return Res.Trim().First().ToString();
            }
            return null;
        }
        public uint GetGroupId(string Path)
        {
            SshCommand sc = client.CreateCommand($"stat -c %g {Path}");
            var Res = sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return Convert.ToUInt32(Res);
            }
            return 0;
        }

        public uint GetAccessRights(string Path)
        {
            SshCommand sc = client.CreateCommand($"stat -c %a {Path}");
            var Res = sc.Execute();
            if (sc.ExitStatus == 0)
            {
                return Utils.ConvertMode(Res.Trim());
            }
            return 0;
        }

        byte[] GetFileContent(string Path)
        {
            SshCommand sc = client.CreateCommand($"cat {Path}");
            var AS = sc.BeginExecute();
            var Started = DateTime.Now;
            while (!AS.IsCompleted)
            {
                var Diff = (DateTime.Now - Started).TotalMilliseconds;
                if (Diff > Timeout)
                    return null;
            }

            if (sc.ExitStatus == 0)
            {
                var MS = new MemoryStream();
                sc.OutputStream.CopyTo(MS);

                sc.EndExecute(AS);

                var Temp = MS.GetBuffer();
                return Temp;
            }

            return null;
        }


    }
}
