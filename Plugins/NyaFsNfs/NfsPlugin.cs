using NyaFs.ImageFormat.Plugins.Base;
using System;

namespace NyaFsNfs
{
    public sealed class NfsPlugin : ServicePlugin
    {
        private int Port = 2049;
        private int MountPort = 20048;
        private int PortmapPort = 111;
        private NfsServer Server;

        public NfsPlugin() : base("nfs", "NFS server")
        {
        }

        protected override void OnStart()
        {
            try
            {
                Server = new NfsServer(getProcessor(), Port, MountPort, PortmapPort);
                Server.Start();

                NyaFs.Log.Ok(3, $"NFS server is running on 0.0.0.0:{Port} (mount {MountPort}, portmap {PortmapPort})");
            }
            catch (Exception E)
            {
                Server?.Stop();
                Server = null;
                NyaFs.Log.Error(0, $"NFS server failed to start: {E.Message}");
            }
        }

        protected override void OnStop()
        {
            Server?.Stop();
            Server = null;
            NyaFs.Log.Ok(0, "NFS server is stopped");
        }

        public override void Info()
        {
            NyaFs.Log.Write(0, "NFS server");
            NyaFs.Log.Write(0, $"       port: {Port}");
            NyaFs.Log.Write(0, $" mount port: {MountPort}");
            NyaFs.Log.Write(0, $"portmapper: {PortmapPort}");
            NyaFs.Log.Write(0, $"    running: {(Server != null ? "yes" : "no")}");
        }

        public override void Usage()
        {
            base.Usage();

            NyaFs.Log.Write(0, $"  set port <port> (NFS, at now {Port})");
            NyaFs.Log.Write(0, $"  set mountport <port> (MOUNT, at now {MountPort})");
            NyaFs.Log.Write(0, $"  set portmap <port> (0 disables, at now {PortmapPort})");
        }

        public override void SetOption(string Param, string Value)
        {
            if (!Int32.TryParse(Value, out int Result))
            {
                NyaFs.Log.Error(0, $"Invalid value {Value} for param {Param}.");
                return;
            }

            switch (Param.ToLowerInvariant())
            {
                case "port":
                    if ((Result > 0) && (Result < 65536))
                    {
                        Port = Result;
                        return;
                    }
                    break;

                case "mountport":
                    if ((Result > 0) && (Result < 65536))
                    {
                        MountPort = Result;
                        return;
                    }
                    break;

                case "portmap":
                    if ((Result >= 0) && (Result < 65536))
                    {
                        PortmapPort = Result;
                        return;
                    }
                    break;
            }

            NyaFs.Log.Error(0, $"Invalid value {Value} for param {Param}.");
        }
    }
}
