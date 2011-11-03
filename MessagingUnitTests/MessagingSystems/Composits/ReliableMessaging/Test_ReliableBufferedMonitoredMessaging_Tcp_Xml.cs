using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.MessagingSystems.Composites;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    [TestFixture]
    public class Test_ReliableBufferedMonitoredMessaging_Tcp_Xml : ReliableMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ISerializer aSerializer = new XmlStringSerializer();

            ChannelId = "tcp://127.0.0.1:6091/";
            ChannelId2 = "tcp://127.0.0.1:6092/";

            IMessagingSystemFactory anUnderlyingMessaging1 = new TcpMessagingSystemFactory();

            UnderlyingMessaging = new BufferedMonitoredMessagingFactory(anUnderlyingMessaging1, aSerializer, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(400));
            
            TimeSpan anAcknowledgementTimeout = TimeSpan.FromMilliseconds(1000);
            ReliableMessagingFactory = new ReliableBufferedMonitoredMessagingFactory(anUnderlyingMessaging1, aSerializer, anAcknowledgementTimeout, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(400));
        }
    }
}
