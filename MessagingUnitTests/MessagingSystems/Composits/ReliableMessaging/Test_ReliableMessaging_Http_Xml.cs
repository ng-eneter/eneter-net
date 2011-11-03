using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    [TestFixture]
    public class Test_ReliableMessaging_Http_Xml : ReliableMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "http://127.0.0.1/Channel1/";
            ChannelId2 = "http://127.0.0.1/Channel2/";
            UnderlyingMessaging = new HttpMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan anAcknowledgementTimeout = TimeSpan.FromMilliseconds(1000);
            ReliableMessagingFactory = new ReliableMessagingFactory(UnderlyingMessaging, aSerializer, anAcknowledgementTimeout);
        }
    }
}
