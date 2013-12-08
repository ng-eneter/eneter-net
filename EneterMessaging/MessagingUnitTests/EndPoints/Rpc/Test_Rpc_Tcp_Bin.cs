using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.Rpc
{
    [TestFixture]
    public class Test_Rpc_Tcp_Bin : RpcBaseTester
    {
        [SetUp]
        public void Setup()
        {
            mySerializer = new BinarySerializer();
            myChannelId = "tcp://127.0.0.1:8095/";
            myMessaging = new TcpMessagingSystemFactory();
        }
    }
}
