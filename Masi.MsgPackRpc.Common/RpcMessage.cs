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
    public abstract class RpcMessage : IPackable, IUnpackable
    {
        public abstract int MessageType { get; }

        protected abstract void PackToMessageCore(Packer packer);
        protected abstract void UnpackFromMessageCore(Unpacker unpacker);

        public void PackToMessage(Packer packer, PackingOptions options)
        {
            packer.Pack(MessageType);
            PackToMessageCore(packer);
        }

        public void UnpackFromMessage(Unpacker unpacker)
        {
            if (MessageType != unpacker.LastReadData.AsInt32())
                throw new InvalidMessagePackStreamException("Invalid message type");

            UnpackFromMessageCore(unpacker);
        }
    }
}
