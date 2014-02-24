using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using NLog;

namespace SharpFtpServer
{
    public class FtpServer : IDisposable
    {
        private bool _disposed = false;
        private bool _listening = false;

        private Logger _log = LogManager.GetCurrentClassLogger();
        private TcpListener _listener;
        private ICollection<ClientConnection> _activeConnections;
        private IPEndPoint _localEndPoint;

        public FtpServer(): this(IPAddress.Any, 21)
        {
        }

        public FtpServer(IPAddress ipAddress, int port)
        {
            _localEndPoint = new IPEndPoint(ipAddress, port);
        }

        public void Start()
        {
            _log.Info("Ftp server v1.0");

            _listener = new TcpListener(_localEndPoint);
            _listening = true;
            _listener.Start();

            _activeConnections = new List<ClientConnection>();

            _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
        }

        public void Stop()
        {
            _log.Info("Stopping Ftp server");

            _listening = false;
            _listener.Stop();

            _listener = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void HandleAcceptTcpClient(IAsyncResult result)
        {
            if (_listening)
            {
                _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
                TcpClient client = _listener.EndAcceptTcpClient(result);
                var connection = new ClientConnection(client);
                _activeConnections.Add(connection);
                ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();

                    foreach (ClientConnection conn in _activeConnections)
                    {
                        conn.Dispose();
                    }
                }
            }

            _disposed = true;
        }
    }
}
