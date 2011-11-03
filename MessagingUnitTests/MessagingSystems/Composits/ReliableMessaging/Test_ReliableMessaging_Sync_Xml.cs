using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    [TestFixture]
    public class Test_ReliableMessaging_Sync_Xml : ReliableMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "Channel_1";
            ChannelId2 = "Channel_2";
            UnderlyingMessaging = new SynchronousMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan anAcknowledgementTimeout = TimeSpan.FromMilliseconds(1000);
            ReliableMessagingFactory = new ReliableMessagingFactory(UnderlyingMessaging, aSerializer,
                anAcknowledgementTimeout);
        }
    }
}
