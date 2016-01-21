using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MsgPack;
using MsgPack.Serialization;

namespace Masi.MsgPackRpc.Common
{
    public enum MessageType : byte
    {
        Request = 1, Response = 2
    }

    public abstract class RpcMessage : IPackable, IUnpackable
    {
        public abstract MessageType RequestType { get; }

        protected abstract void PackToMessageCore(Packer packer);
        protected abstract void UnpackFromMessageCore(Unpacker unpacker);

        public void PackToMessage(Packer packer, PackingOptions options)
        {
            packer.Pack((byte)RequestType);

            PackToMessageCore(packer);
        }

        public void UnpackFromMessage(Unpacker unpacker)
        {
            if (RequestType != (MessageType)unpacker.LastReadData.AsByte())
                throw new InvalidMessagePackStreamException("Invalid message type");

            UnpackFromMessageCore(unpacker);
        }
    }
}
