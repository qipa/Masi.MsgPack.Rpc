using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Masi.MsgPackRpc.Server
{
    class TcpTransportManager
    {
        private readonly IRpcServerContext _serverContext;
        private readonly Socket _sock;
        private readonly ConcurrentDictionary<int, TcpTransportChannel> _tcpChannels 
            = new ConcurrentDictionary<int, TcpTransportChannel>();

        // TODO: configurable
        private const int MIN_ACCEPT_CONCURRENCY = 5;

        public TcpTransportManager(IPEndPoint endPoint, IRpcServerContext serverContext)
        {
            _sock = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sock.Bind(endPoint);
            _sock.Listen(100);

            _serverContext = serverContext;

            for (int i = 0; i < MIN_ACCEPT_CONCURRENCY; i++)
            {
                BeginAccept();
            }
        }

        private void BeginAccept()
        {
            _sock.BeginAccept(OnAccept, null);
        }

        private void OnAccept(IAsyncResult res)
        {
            Socket socket = _sock.EndAccept(res);

            var tcpChannel = new TcpTransportChannel(socket, _serverContext);
            _tcpChannels.TryAdd(tcpChannel.Id, tcpChannel);

            BeginAccept();
        }
    }
}
