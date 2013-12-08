using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.EndPoints.TypedMessages;

namespace Eneter.MessagingUnitTests.EndPoints.SyncTypedMessages
{
    [TestFixture]
    public class Test_SyncTypedMessages_Tcp_Xml : SyncTypedMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            string aPort = RandomPortGenerator.Generate();
            string anAddress = "tcp://127.0.0.1:" + aPort + "/";

            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
            InputChannel = aMessaging.CreateDuplexInputChannel(anAddress);
            OutputChannel = aMessaging.CreateDuplexOutputChannel(anAddress);

            DuplexTypedMessagesFactory = new DuplexTypedMessagesFactory();
        }
    }
}
