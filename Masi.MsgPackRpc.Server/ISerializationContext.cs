using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Server
{
    interface ISerializationContext
    {
        MessagePackSerializer<RpcMessage> Serializer { get; }
    }
}
