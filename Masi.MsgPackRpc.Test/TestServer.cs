using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using MsgPack;
using MsgPack.Serialization;
using Masi.MsgPackRpc.Server;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Test
{
    class TestServer : RpcServer
    {
        public TestServer(IPEndPoint endpoint) : base(endpoint) { }

        protected override RpcMessageSerializer CreateMessageSerializer(SerializationContext ownerContext)
        {
            return new TestSerializer(ownerContext);
        }

        protected override SerializationContext CreateSerializationContext()
        {
            return new SerializationContext();
        }
    }

    class TestSerializer : RpcMessageSerializer
    {
        public TestSerializer(SerializationContext ownerContext)
            : base(ownerContext)
        {
        }

        public TestSerializer(SerializationContext ownerContext, PackerCompatibilityOptions packerCompatibilityOptions)
            : base(ownerContext, packerCompatibilityOptions)
        {
        }

        protected override Type GetRpcMessageType(int typeCode)
        {
            if (typeCode == 1)
                return typeof(TestMessage);
            else
                throw new ArgumentException("typeCode");
        }
    }

    class TestMessage : RpcMessage
    {
        private string _str = "Asd";

        public override int RpcType
        {
            get { return 1; }
        }

        protected override void PackToMessageCore(Packer packer)
        {
            packer.PackArrayHeader(1);
            packer.Pack(_str);
        }

        protected override void UnpackFromMessageCore(Unpacker unpacker)
        {
            RpcMessageSerializer.ReadNext(unpacker);
            _str = (string)unpacker.LastReadData;
        }
    }
}
