using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Server
{
    public interface IChannelRequest
    {
        int ChannelId { get; }
        RpcRequest Request { get; }

        void SendResponse(RpcResponse response);
    }

    internal class ChannelRequest : IChannelRequest
    {
        private readonly TcpTransportChannel _channel;
        private readonly int _frameId;
        private readonly RpcRequest _request;

        public ChannelRequest(TcpTransportChannel channel, int frameId, RpcRequest request)
        {
            _channel = channel;
            _frameId = frameId;
            _request = request;
        }

        public int ChannelId
        {
            get { return _channel.Id; }
        }

        public int FrameId
        {
            get { return _frameId; }
        }

        public RpcRequest Request
        {
            get { return _request; }
        }

        public void SendResponse(RpcResponse response)
        {
            _channel.SendResponse(this, response);
        }
    }
}
