using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NyaFsNfs.Protocol
{
    internal sealed class RpcTcpService : IDisposable
    {
        private readonly string Name;
        private readonly int Port;
        private readonly Func<RpcCall, byte[]> Handler;
        private readonly object Lock = new object();
        private readonly List<TcpClient> Clients = new List<TcpClient>();

        private TcpListener Listener;
        private CancellationTokenSource Cancellation;

        public int BoundPort => (Listener?.LocalEndpoint as IPEndPoint)?.Port ?? Port;

        public RpcTcpService(string Name, int Port, Func<RpcCall, byte[]> Handler)
        {
            this.Name = Name;
            this.Port = Port;
            this.Handler = Handler;
        }

        public void Start()
        {
            if (Listener != null)
                return;

            Cancellation = new CancellationTokenSource();
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();

            Task.Run(() => AcceptLoop(Cancellation.Token));
        }

        public void Stop()
        {
            try { Cancellation?.Cancel(); } catch { }
            try { Listener?.Stop(); } catch { }
            Listener = null;

            lock (Lock)
            {
                foreach (var Client in Clients)
                {
                    try { Client.Close(); } catch { }
                }

                Clients.Clear();
            }

            Cancellation?.Dispose();
            Cancellation = null;
        }

        public void Dispose() => Stop();

        private async Task AcceptLoop(CancellationToken Token)
        {
            while (!Token.IsCancellationRequested)
            {
                TcpClient Client;

                try
                {
                    Client = await Listener.AcceptTcpClientAsync();
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

                lock (Lock)
                    Clients.Add(Client);

                _ = Task.Run(() => HandleClient(Client, Token));
            }
        }

        private void HandleClient(TcpClient Client, CancellationToken Token)
        {
            try
            {
                Client.NoDelay = true;
                using (var Stream = Client.GetStream())
                {
                    while (!Token.IsCancellationRequested)
                    {
                        var Message = ReadRecord(Stream);
                        if (Message == null)
                            return;

                        if (!Rpc.TryParse(Message, out RpcCall Call))
                            continue;

                        byte[] Reply = null;
                        try
                        {
                            Reply = Handler(Call);
                        }
                        catch
                        {
                            Reply = Rpc.Error(Call.Xid, Rpc.AcceptSystemError);
                        }

                        if (Reply != null)
                            WriteRecord(Stream, Reply);
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
            catch
            {
            }
            finally
            {
                lock (Lock)
                    Clients.Remove(Client);

                try { Client.Close(); } catch { }
            }
        }

        private static byte[] ReadRecord(NetworkStream Stream)
        {
            using (var Output = new MemoryStream())
            {
                while (true)
                {
                    var Header = ReadExactly(Stream, 4);
                    if (Header == null)
                        return null;

                    uint Mark = ((uint)Header[0] << 24) | ((uint)Header[1] << 16) |
                                ((uint)Header[2] << 8) | Header[3];
                    bool Last = (Mark & 0x80000000u) != 0;
                    int Length = (int)(Mark & 0x7FFFFFFFu);

                    if ((Length < 0) || (Length > 16 * 1024 * 1024))
                        throw new IOException("RPC record is too long");

                    var Fragment = ReadExactly(Stream, Length);
                    if (Fragment == null)
                        return null;

                    Output.Write(Fragment, 0, Fragment.Length);

                    if (Last)
                        return Output.ToArray();
                }
            }
        }

        private static void WriteRecord(NetworkStream Stream, byte[] Message)
        {
            var Header = new byte[4];
            uint Mark = 0x80000000u | (uint)Message.Length;
            Header[0] = (byte)(Mark >> 24);
            Header[1] = (byte)(Mark >> 16);
            Header[2] = (byte)(Mark >> 8);
            Header[3] = (byte)Mark;

            Stream.Write(Header, 0, Header.Length);
            Stream.Write(Message, 0, Message.Length);
            Stream.Flush();
        }

        private static byte[] ReadExactly(NetworkStream Stream, int Length)
        {
            if (Length == 0)
                return Array.Empty<byte>();

            var Result = new byte[Length];
            int Position = 0;

            while (Position < Length)
            {
                int Read = Stream.Read(Result, Position, Length - Position);
                if (Read <= 0)
                    return null;

                Position += Read;
            }

            return Result;
        }
    }
}
