using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Client
{
    public abstract class RpcClient
    {
        private readonly Socket _sock;
        private readonly object _lock = new object(); 

        private volatile MessagePackSerializer<RpcMessage> _serializer;

        public RpcClient(IPEndPoint endPoint)
        {
            _sock = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _sock.BeginConnect(endPoint, OnConnect, null);
        }

        protected abstract SerializationContext CreateSerializationContext();
        protected abstract RpcMessageSerializer CreateMessageSerializer(SerializationContext ownerContext);

        private MessagePackSerializer<RpcMessage> Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    lock (_lock)
                    {
                        if (_serializer == null) // Double lock check
                        {
                            var context = CreateSerializationContext();
                            context.Serializers.Register<RpcMessage>(CreateMessageSerializer(context));

                            _serializer = MessagePackSerializer.Get<RpcMessage>(context);
                        }
                    }
                }

                return _serializer;
            }
        }

        private void OnConnect(IAsyncResult res)
        {
            _sock.EndConnect(res);
        }

        public void TestSend(RpcMessage message)
        {
            var bytes = Serializer.PackSingleObject(message);
            _sock.Send(new byte[] { 1, 0, 0, 0 }); // Version + reserved + frame id + protocol code
            _sock.Send(BitConverter.GetBytes(bytes.Length)); // Message len
            _sock.Send(bytes);
        }
    }
}
