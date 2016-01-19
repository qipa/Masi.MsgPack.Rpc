using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Test
{
    [TestClass]
    public class ByteArraySegmentedStreamTests
    {
        [TestMethod]
        public void ByteArraySegmentedStream_ReadByteTest()
        {
            var arr = new byte[100];
            for (byte i = 0; i < 100; i++)
                arr[i] = i;

            var segments = new List<ArraySegment<byte>>();
            

            var stream = new ByteArraySegmentedStream(true);
            stream.AddSegment(new ArraySegment<byte>(arr, 0, 5));
            stream.AddSegment(new ArraySegment<byte>(arr, 2, 5));
            stream.AddSegment(new ArraySegment<byte>(arr, 10, 10));

            var reader = new BinaryReader(stream);

            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i, reader.ReadByte());

            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i + 2, reader.ReadByte());

            for (byte i = 0; i < 10; i++)
                Assert.AreEqual(i + 10, reader.ReadByte());
        }

        [TestMethod]
        public void ByteArraySegmentedStream_ReadBytesTest()
        {
            var arr = new byte[100];
            for (byte i = 0; i < 100; i++)
                arr[i] = i;

            var stream = new ByteArraySegmentedStream(true);
            stream.AddSegment(new ArraySegment<byte>(arr, 0, 10));
            stream.AddSegment(new ArraySegment<byte>(arr, 10, 25));
            stream.AddSegment(new ArraySegment<byte>(arr, 5, 10));

            var reader = new BinaryReader(stream);

            var bs = reader.ReadBytes(5);
            Assert.AreEqual(bs.Length, 5);
            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i, bs[i]);

            bs = reader.ReadBytes(10);
            Assert.AreEqual(bs.Length, 10);
            for (byte i = 0; i < 10; i++)
                Assert.AreEqual(i + 5, bs[i]);

            bs = reader.ReadBytes(100);
            Assert.AreEqual(bs.Length, 30);
            for (byte i = 0; i < 20; i++)
                Assert.AreEqual(i + 15, bs[i]);
            for (byte i = 0; i < 10; i++)
                Assert.AreEqual(i + 5, bs[i + 20]);
        }

        [TestMethod]
        public void ByteArraySegmentedStream_SeekTest()
        {
            var arr = new byte[100];
            for (byte i = 0; i < 100; i++)
                arr[i] = i;

            var stream = new ByteArraySegmentedStream(false);
            stream.AddSegment(new ArraySegment<byte>(arr, 0, 5));
            stream.AddSegment(new ArraySegment<byte>(arr, 2, 5));
            stream.AddSegment(new ArraySegment<byte>(arr, 10, 10));

            var reader = new BinaryReader(stream);

            long pos = stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, pos);

            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i, reader.ReadByte());

            pos = stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, pos);

            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i, reader.ReadByte());

            pos = stream.Seek(-15, SeekOrigin.End);
            Assert.AreEqual(5, pos);

            for (byte i = 0; i < 5; i++)
                Assert.AreEqual(i + 2, reader.ReadByte());
            for (byte i = 0; i < 10; i++)
                Assert.AreEqual(i + 10, reader.ReadByte());

            Assert.AreEqual(0, reader.ReadBytes(10).Length);

            stream.Position -= 6;
            Assert.AreEqual(14, reader.ReadByte());

            stream.Position += 5;
            Assert.AreEqual(0, reader.ReadBytes(10).Length);
        }
    }
}
