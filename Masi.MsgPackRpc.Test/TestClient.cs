using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Client;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Test
{
    class TestClient : RpcClient
    {
        public TestClient(IPEndPoint endPoint)
            : base(endPoint)
        {
        }

        protected override SerializationContext CreateSerializationContext()
        {
            return new SerializationContext();
        }

        protected override RpcMessageSerializer CreateMessageSerializer(SerializationContext ownerContext)
        {
            return new TestSerializer(ownerContext);
        }
    }
}
