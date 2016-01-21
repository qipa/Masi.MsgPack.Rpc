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

        private readonly ConcurrentQueue<ArraySegment<byte>> _recvQueue = new ConcurrentQueue<ArraySegment<byte>>();
        private readonly AutoResetEvent _recvEvent = new AutoResetEvent(false);

        private byte[] _recvBuffer;
        private int _recvBufferOffset;

        // TODO: configurable
        private const int BUFFER_SIZE = 65536;
        private const int MAX_BODY_SIZE = 262144;

        public TcpTransportChannel(Socket socket, IRpcServerContext serverContext)
        {
            _id = Interlocked.Increment(ref _idCounter);
            _sock = socket;

            _serverContext = serverContext;

            _recvBuffer = new byte[BUFFER_SIZE];
            _recvBufferOffset = 0;

            BeginReceive(BUFFER_SIZE);

            new Thread(ReadReceiveBuffer) { IsBackground = true }.Start();
        }

        public int Id
        {
            get { return _id; }
        }

        private void BeginReceive(int numBytes)
        {
            _sock.BeginReceive(_recvBuffer, _recvBufferOffset, numBytes, SocketFlags.None, OnReceive, null);
        }

        private void OnReceive(IAsyncResult res)
        {
            int received = _sock.EndReceive(res);

            _recvQueue.Enqueue(new ArraySegment<byte>(_recvBuffer, _recvBufferOffset, received));
            _recvEvent.Set();

            _recvBufferOffset += received;

            if (_recvBufferOffset == BUFFER_SIZE)
            {
                _recvBuffer = new byte[BUFFER_SIZE];
                _recvBufferOffset = 0;
            }

            BeginReceive(BUFFER_SIZE - _recvBufferOffset);
        }

        private void ReadReceiveBuffer()
        {
            var stream = new ByteArraySegmentedStream();
            var reader = new BinaryReader(stream);

            int recvBytes = 0;
            RpcFrame frame = null;

            while (true)
            {
                _recvEvent.WaitOne();

                // Read next message

                ArraySegment<byte> segment;
                while (_recvQueue.TryDequeue(out segment))
                {
                    stream.AddSegment(segment);
                    recvBytes += segment.Count;

                    ProcessFrames(reader, ref recvBytes, ref frame);
                }
            }
        }

        private void ProcessFrames(BinaryReader reader, ref int recvBytes, ref RpcFrame frame)
        {
            bool continueRead = false;

            do
            {
                if (frame == null && recvBytes >= 8)
                {
                    // Frame received
                    byte version = reader.ReadByte();
                    byte reserved = reader.ReadByte();
                    byte frameId = reader.ReadByte();
                    byte protocolCode = reader.ReadByte();
                    int bodyLen = reader.ReadInt32();

                    if (version == 1 && reserved == 0 && protocolCode == 0)
                    {
                        if (bodyLen >= 0 && bodyLen <= MAX_BODY_SIZE)
                        {
                            frame = new RpcFrame();
                            frame.FrameId = frameId;
                            frame.BodyLen = bodyLen;
                        }
                        else
                        {
                            // Invalid header
                        }
                    }
                    else
                    {
                        // Invalid header, close the connection
                    }

                    recvBytes -= 8;
                    continueRead = true;
                }

                if (frame != null)
                {
                    if (frame.BodyLen == 0)
                    {
                        frame = null; // Start reading of next frame
                        continueRead = true;
                    }
                    else if (recvBytes >= frame.BodyLen)
                    {
                        // Frame body received

                        byte[] frameBody = reader.ReadBytes(frame.BodyLen);

                        MessagePackSerializer<RpcMessage> serializer = _serverContext.SerializationContext.Serializer;
                        RpcRequest request = (RpcRequest)serializer.UnpackSingleObject(frameBody);

                        ChannelRequest channelRequest = new ChannelRequest(this, frame.FrameId, request);

                        _serverContext.RequestDispatcher.DispatchRequest(channelRequest);

                        recvBytes -= frame.BodyLen;
                        frame = null; // Start reading of next frame
                        continueRead = true;
                    }
                }
            }
            while (continueRead);
        }

        internal void SendResponse(ChannelRequest request, RpcResponse response)
        {
            MessagePackSerializer<RpcMessage> serializer = _serverContext.SerializationContext.Serializer;

            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)request.FrameId);
                writer.Write((byte)0);
                writer.Write((int)0); // Dummy value replaced

                int prevPos = (int)stream.Position;
                serializer.Pack(stream, response);
                int bodyLen = (int)stream.Position - prevPos;

                // Set the body len to frame head

                stream.Seek(4, SeekOrigin.Begin);
                writer.Write((int)bodyLen);

                byte[] buffer = stream.GetBuffer();
                _sock.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSend, null);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            _sock.EndSend(ar);
        }

        private class RpcFrame
        {
            public byte FrameId;
            public int BodyLen;
        }
    }
}
