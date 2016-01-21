using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;

namespace Masi.MsgPackRpc.Common
{
    public abstract class RpcResponse : RpcMessage
    {
        public override MessageType RequestType
        {
            get { return MessageType.Response; }
        }
    }
}
