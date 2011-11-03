using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    [TestFixture]
    public class Test_ReliableBufferedMonitoredMessaging_Http_Xml : ReliableMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ISerializer aSerializer = new XmlStringSerializer();

            ChannelId = "http://127.0.0.1/Channel1/";
            ChannelId2 = "http://127.0.0.1/Channel2/";

            // Note: Polling frequency time must be less then timeout for the ping response.
            IMessagingSystemFactory anUnderlyingMessaging1 = new HttpMessagingSystemFactory(250, 30000);

            UnderlyingMessaging = new BufferedMonitoredMessagingFactory(anUnderlyingMessaging1, aSerializer, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(400));

            TimeSpan anAcknowledgementTimeout = TimeSpan.FromMilliseconds(1000);
            ReliableMessagingFactory = new ReliableBufferedMonitoredMessagingFactory(anUnderlyingMessaging1, aSerializer, anAcknowledgementTimeout, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(400));
        }
    }
}
