
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;


namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    [TestFixture]
    public class Test_ReliableMessaging_Tcp_Xml : ReliableMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "tcp://127.0.0.1:6091/";
            ChannelId2 = "tcp://127.0.0.1:6092/";
            UnderlyingMessaging = new TcpMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan anAcknowledgementTimeout = TimeSpan.FromMilliseconds(1000);
            ReliableMessagingFactory = new ReliableMessagingFactory(UnderlyingMessaging, aSerializer, anAcknowledgementTimeout);
        }
    }
}

#endif