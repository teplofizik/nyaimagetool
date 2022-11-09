﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FxSsh
{
    public class SshServer : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<Session> sessions = new List<Session>();
        private readonly Dictionary<string, string> hostKeys = new Dictionary<string, string>();
        private bool _isDisposed;
        private bool _started;
        private TcpListener _listener = null;

        public SshServer()
            : this(new SshServerSettings())
        { }

        public SshServer(SshServerSettings settings)
        {
            Contract.Requires(settings != null);

            ServerSettings = settings;
        }

        public SshServerSettings ServerSettings { get; private set; }

        public event EventHandler<Session> ConnectionAccepted;

        public event EventHandler<Exception> ExceptionRaised;

        public void Start()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (_started)
                    throw new InvalidOperationException("The server is already started.");

                _listener = ServerSettings.LocalAddress == IPAddress.IPv6Any
                    ? TcpListener.Create(ServerSettings.Port) // dual stack
                    : new TcpListener(ServerSettings.LocalAddress, ServerSettings.Port);
                _listener.ExclusiveAddressUse = false;
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                BeginAcceptSocket();

                _started = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (!_started)
                    throw new InvalidOperationException("The server is not started.");

                _listener.Stop();

                _isDisposed = true;
                _started = false;

                try
                {
                    foreach (var session in sessions)
                    {
                        try
                        {
                            session.Disconnect();
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public void AddHostKey(string type, string key)
        {
            Contract.Requires(type != null);
            Contract.Requires(key != null);

            if (!hostKeys.ContainsKey(type))
                hostKeys.Add(type, key);
        }

        private void BeginAcceptSocket()
        {
            try
            {
                _listener.BeginAcceptSocket(AcceptSocket, null);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch
            {
                if (_started)
                    BeginAcceptSocket();
            }
        }

        private void AcceptSocket(IAsyncResult ar)
        {
            try
            {
                var socket = _listener.EndAcceptSocket(ar);


                Task.Run(() =>
                {
                    var session = new Session(socket, hostKeys, ServerSettings.ServerBanner, ServerSettings.IdleTimeout);
                    session.Disconnected += (ss, ee) => { lock (_lock)
                            
                            sessions.Remove(session); 
                    };
                    lock (_lock)
                        sessions.Add(session);
                    try
                    {
                        ConnectionAccepted?.Invoke(this, session);
                        session.EstablishConnection();
                    }
                    catch (SshConnectionException ex)
                    {
                        session.Disconnect(ex.DisconnectReason, ex.Message);
                        ExceptionRaised?.Invoke(this, ex);
                    }
                    catch (Exception ex)
                    {
                        session.Disconnect();
                        ExceptionRaised?.Invoke(this, ex);
                    }
                });
            }
            catch
            {
            }
            finally
            {
                BeginAcceptSocket();
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #region IDisposable

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                Stop();
            }
        }

        #endregion IDisposable
    }
}