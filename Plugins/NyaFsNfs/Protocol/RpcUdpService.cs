using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NyaFsNfs.Protocol
{
    internal sealed class RpcUdpService : IDisposable
    {
        private readonly int Port;
        private readonly Func<RpcCall, byte[]> Handler;
        private UdpClient Client;
        private CancellationTokenSource Cancellation;

        public RpcUdpService(string Name, int Port, Func<RpcCall, byte[]> Handler)
        {
            this.Port = Port;
            this.Handler = Handler;
        }

        public void Start()
        {
            if (Client != null)
                return;

            Cancellation = new CancellationTokenSource();
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
            Task.Run(() => Loop(Cancellation.Token));
        }

        public void Stop()
        {
            try { Cancellation?.Cancel(); } catch { }
            try { Client?.Close(); } catch { }
            Client = null;
            Cancellation?.Dispose();
            Cancellation = null;
        }

        public void Dispose() => Stop();

        private void Loop(CancellationToken Token)
        {
            while (!Token.IsCancellationRequested)
            {
                IPEndPoint Remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] Data;

                try
                {
                    Data = Client.Receive(ref Remote);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }
                catch
                {
                    if (Token.IsCancellationRequested)
                        return;

                    continue;
                }

                if (!Rpc.TryParse(Data, out RpcCall Call))
                    continue;

                byte[] Reply;
                try
                {
                    Reply = Handler(Call);
                }
                catch
                {
                    Reply = Rpc.Error(Call.Xid, Rpc.AcceptSystemError);
                }

                if (Reply != null)
                {
                    try
                    {
                        Client.Send(Reply, Reply.Length, Remote);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
