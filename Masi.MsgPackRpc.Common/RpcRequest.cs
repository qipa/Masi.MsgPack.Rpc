using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;

namespace Masi.MsgPackRpc.Common
{
    public abstract class RpcRequest : RpcMessage
    {
        public override MessageType RequestType
        {
            get { return MessageType.Request; }
        }
    }
}
