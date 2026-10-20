using NyaFs.Processor;
using NyaFsNfs.Protocol;
using System;

namespace NyaFsNfs
{
    internal sealed class NfsServer : IDisposable
    {
        public int NfsPort { get; }
        public int MountPort { get; }
        public int PortmapPort { get; }

        private readonly RpcTcpService NfsService;
        private readonly RpcTcpService MountTcpService;
        private readonly RpcTcpService PortmapTcpService;
        private readonly RpcUdpService NfsUdpService;
        private readonly RpcUdpService MountUdpService;
        private readonly RpcUdpService PortmapUdpService;

        public NfsServer(ImageProcessor Processor, int NfsPort, int MountPort, int PortmapPort)
        {
            this.NfsPort = NfsPort;
            this.MountPort = MountPort;
            this.PortmapPort = PortmapPort;

            var Fs = new NfsFs(Processor);
            var Nfs = new NfsV3(Fs);
            var Mount = new MountService(Fs);
            var Portmap = new PortmapService(NfsPort, MountPort, PortmapPort);

            NfsService = new RpcTcpService("nfs", NfsPort, Nfs.Dispatch);
            MountTcpService = new RpcTcpService("mount", MountPort, Mount.Dispatch);
            NfsUdpService = new RpcUdpService("nfs", NfsPort, Nfs.Dispatch);
            MountUdpService = new RpcUdpService("mount", MountPort, Mount.Dispatch);

            if (PortmapPort > 0)
            {
                PortmapTcpService = new RpcTcpService("portmap", PortmapPort, Portmap.Dispatch);
                PortmapUdpService = new RpcUdpService("portmap", PortmapPort, Portmap.Dispatch);
            }
        }

        public void Start()
        {
            NfsService.Start();
            MountTcpService.Start();
            NfsUdpService.Start();
            MountUdpService.Start();

            if (PortmapTcpService != null)
            {
                try
                {
                    PortmapTcpService.Start();
                    PortmapUdpService.Start();
                }
                catch (Exception E)
                {
                    NyaFs.Log.Warning(0, $"NFS portmapper is not available on port {PortmapPort}: {E.Message}");
                }
            }
        }

        public void Stop()
        {
            PortmapUdpService?.Stop();
            PortmapTcpService?.Stop();
            MountUdpService?.Stop();
            MountTcpService?.Stop();
            NfsUdpService?.Stop();
            NfsService?.Stop();
        }

        public void Dispose() => Stop();
    }
}
