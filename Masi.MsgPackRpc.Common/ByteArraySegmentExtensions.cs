using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Masi.MsgPackRpc.Common
{
    public static class ByteArraySegmentExtensions
    {
        public static void CopyTo(this ArraySegment<byte> segment, byte[] destination, long destinationIndex, long length)
        {
            if (length > segment.Count)
            {
                throw new ArgumentException("length can not exceed segment count", "length");
            }

            Array.Copy(segment.Array, segment.Offset, destination, destinationIndex, length);
        }

        public static void CopyTo(this ArraySegment<byte> segment, long segmentOffset, byte[] destination, long destinationIndex, long length)
        {
            if (length > segment.Count)
            {
                throw new ArgumentException("length can not exceed segment count", "length");
            }

            Array.Copy(segment.Array, segment.Offset + segmentOffset, destination, destinationIndex, length);
        }
    }
}
