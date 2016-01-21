using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using MsgPack;
using MsgPack.Serialization;

namespace Masi.MsgPackRpc.Common
{
    public abstract class RpcMessageSerializer : MessagePackSerializer<RpcMessage>
    {
        private static PackingOptions _opts = new PackingOptions() { StringEncoding = Encoding.UTF8 };

        protected RpcMessageSerializer(SerializationContext ownerContext)
            : base(ownerContext)
        {
        }

        protected RpcMessageSerializer(SerializationContext ownerContext, PackerCompatibilityOptions packerCompatibilityOptions)
            : base(ownerContext, packerCompatibilityOptions)
        {
        }

        protected override void PackToCore(Packer packer, RpcMessage objectTree)
        {
            objectTree.PackToMessage(packer, _opts);
        }

        protected override RpcMessage UnpackFromCore(Unpacker unpacker)
        {
            bool isRequest = ((MessageType)unpacker.LastReadData.AsByte()) == MessageType.Request;

            RpcMessage message;
            if (isRequest)
            {
                message = InitializeRequestMessage(unpacker);
            }
            else
            {
                message = InitializeResponseMessage(unpacker);
            }

            message.UnpackFromMessage(unpacker);

            return message;
        }

        public static void ReadNext(Unpacker unpacker)
        {
            if (!unpacker.Read())
            {
                throw new SerializationException("Message unexpectedly ends");
            }
        }

        protected abstract RpcRequest InitializeRequestMessage(Unpacker unpacker);
        protected abstract RpcResponse InitializeResponseMessage(Unpacker unpacker);
    }
}
