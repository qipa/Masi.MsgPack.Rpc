using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Server
{
    class TcpTransportChannel
    {
        private static int _idCounter = 0;

        private readonly Socket _sock;
        private readonly IRpcServerContext _serverContext;
        private readonly int _id;
        private readonly ConcurrentQueue<ArraySegment<byte>> _recvQueue;

        private byte[] _currentBuffer;
        private int _currentBufferOffset;

        private readonly AutoResetEvent _recvEvent = new AutoResetEvent(false);

        private const int BUFFER_SIZE = 65536;

        public TcpTransportChannel(Socket socket, IRpcServerContext serverContext)
        {
            _id = Interlocked.Increment(ref _idCounter);
            _sock = socket;

            _serverContext = serverContext;

            _recvQueue = new ConcurrentQueue<ArraySegment<byte>>();
            _currentBuffer = new byte[BUFFER_SIZE];
            _currentBufferOffset = 0;

            BeginReceive(BUFFER_SIZE);

            new Thread(ReadReceiveBuffer) { IsBackground = true }.Start();
        }

        public int Id
        {
            get { return _id; }
        }

        private void BeginReceive(int numBytes)
        {
            _sock.BeginReceive(_currentBuffer, _currentBufferOffset, numBytes, SocketFlags.None, OnReceive, null);
        }

        private void OnReceive(IAsyncResult res)
        {
            int received = _sock.EndReceive(res);

            _recvQueue.Enqueue(new ArraySegment<byte>(_currentBuffer, _currentBufferOffset, received));
            _recvEvent.Set();

            _currentBufferOffset += received;

            if (_currentBufferOffset == BUFFER_SIZE)
            {
                _currentBuffer = new byte[BUFFER_SIZE];
                _currentBufferOffset = 0;
            }

            BeginReceive(BUFFER_SIZE - _currentBufferOffset);
        }

        private void ReadReceiveBuffer()
        {
            var stream = new ByteArraySegmentedStream(true);
            var reader = new BinaryReader(stream);

            int recvBytes = 0;
            int? msgLen = null;

            while (true)
            {
                _recvEvent.WaitOne();

                // Read next message

                ArraySegment<byte> segment;
                while (_recvQueue.TryDequeue(out segment))
                {
                    stream.AddSegment(segment);
                    recvBytes += segment.Count;

                    while (stream.Length > 0)
                    {
                        long prevPos = stream.Position;

                        if (!msgLen.HasValue && recvBytes >= 16)
                        {
                            byte version = reader.ReadByte();
                            short reserved = reader.ReadInt16();
                            byte protocolCode = reader.ReadByte();

                            if (version == 1 && reserved == 0 && protocolCode == 0)
                            {
                                ushort messageId = reader.ReadUInt16();
                                ushort messageType = reader.ReadUInt16();

                                msgLen = reader.ReadInt32();

                                if (msgLen.Value < 0)
                                {
                                    // Invalid header
                                }
                            }
                            else
                            {
                                // Invalid header, close the connection
                            }

                            recvBytes -= 16;
                        }

                        if (msgLen.HasValue && recvBytes >= msgLen.Value)
                        {
                            // Full message received

                            var messageBody = reader.ReadBytes(msgLen.Value);

                            MessagePackSerializer<RpcMessage> serializer = _serverContext.SerializationContext.Serializer;
                            RpcMessage message = serializer.UnpackSingleObject(messageBody);

                            _serverContext.MessageDispatcher.DispatchMessage(message);
                            
                            recvBytes -= msgLen.Value;
                            msgLen = null; // Indicate to start reading of next frame
                        }

                        if (stream.Position == prevPos)
                            break; // Nothing read, continue dequeing segments
                    }
                }
            }
        }
    }
}
