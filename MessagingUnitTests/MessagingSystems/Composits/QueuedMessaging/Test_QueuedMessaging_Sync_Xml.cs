using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.QueuedMessaging
{
    [TestFixture]
    public class Test_QueuedMessaging_Sync_Xml : QueuedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "Channel_1";
            UnderlyingMessaging = new SynchronousMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(UnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 5;
        }
    }
}
