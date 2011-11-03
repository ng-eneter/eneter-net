#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.QueuedMessaging
{
    [TestFixture]
    public class Test_QueuedMessaging_Http_Xml : QueuedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "http://127.0.0.1/bbb/";
            UnderlyingMessaging = new HttpMessagingSystemFactory(200, 600000);
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(UnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 500;
        }
    }
}

#endif