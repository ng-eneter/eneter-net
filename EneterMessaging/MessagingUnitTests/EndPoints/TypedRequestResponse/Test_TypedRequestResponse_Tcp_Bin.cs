using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedRequestResponse
{
    [TestFixture]
    public class Test_TypedRequestResponse_Tcp_Bin : TypedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory();

            // Generate random number for the port.
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000);
            string aChannelId = "tcp://127.0.0.1:" + aPort + "/";

            ISerializer aSerializer = new BinarySerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}
