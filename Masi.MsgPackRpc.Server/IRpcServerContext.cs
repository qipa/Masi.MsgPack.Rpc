using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Masi.MsgPackRpc.Server
{
    interface IRpcServerContext
    {
        ISerializationContext SerializationContext { get; }
        IMessageDispatcher MessageDispatcher { get; }
    }
}
