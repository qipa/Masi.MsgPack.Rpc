using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Server
{
    public abstract class RpcServer
    {
        private readonly RpcServerContext _serverContext;
        private readonly TcpTransportManager _tcpManager;

        public RpcServer(IPEndPoint endPoint)
        {
            _serverContext = new RpcServerContext(this);
            _tcpManager = new TcpTransportManager(endPoint, _serverContext);
        }

        protected abstract SerializationContext CreateSerializationContext();
        protected abstract RpcMessageSerializer CreateMessageSerializer(SerializationContext ownerContext);
        protected abstract IRequestDispatcher CreateRequestDispatcher();


        private class RpcServerContext : IRpcServerContext
        {
            private readonly RpcServer _server;
            private readonly ISerializationContext _serializationContext;
            private readonly object _lock = new object();

            private volatile IRequestDispatcher _requestDispatcher;

            public RpcServerContext(RpcServer server)
            {
                _server = server;
                _serializationContext = new RpcSerializationContext(_server);
            }

            public ISerializationContext SerializationContext
            {
                get { return _serializationContext; }
            }

            public IRequestDispatcher RequestDispatcher
            {
                get 
                { 
                    if (_requestDispatcher == null)
                    {
                        lock (_lock)
                        {
                            if (_requestDispatcher == null) // Double lock check
                            {
                                _requestDispatcher = _server.CreateRequestDispatcher();
                            }
                        }
                    }

                    return _requestDispatcher;
                }
            }
        }

        private class RpcSerializationContext : ISerializationContext
        {
            private readonly RpcServer _server;
            private readonly object _lock = new object();

            private volatile MessagePackSerializer<RpcMessage> _serializer;

            public RpcSerializationContext(RpcServer server)
            {
                _server = server;
            }

            public MessagePackSerializer<RpcMessage> Serializer
            {
                get 
                { 
                    if (_serializer == null)
                    {
                        lock (_lock)
                        {
                            if (_serializer == null) // Double lock check
                            {
                                var context = _server.CreateSerializationContext();
                                context.Serializers.Register<RpcMessage>(_server.CreateMessageSerializer(context));

                                _serializer = MessagePackSerializer.Get<RpcMessage>(context);
                            }
                        }
                    }

                    return _serializer;
                }
            }
        }
    }
}
