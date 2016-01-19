using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Masi.MsgPackRpc.Server;
using Masi.MsgPackRpc.Client;

namespace Masi.MsgPackRpc.Test
{
    [TestClass]
    public class ClientServerTests
    {
        [TestMethod]
        public void ClientServerTest()
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, 12000);

            var server = new TestServer(endPoint);

            System.Threading.Thread.Sleep(1000);

            var client = new TestClient(endPoint);

            for (int i = 0; i < 10; i++)
            {
                client.TestSend(new TestMessage());
            }

            System.Threading.Thread.Sleep(1000000);
        }
    }
}
