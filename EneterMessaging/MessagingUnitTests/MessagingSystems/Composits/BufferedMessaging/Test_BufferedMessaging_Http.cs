#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_BufferedMessaging_Http : BufferedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "http://127.0.0.1:8056/bbb/";
            IMessagingSystemFactory anUnderlyingMessaging = new HttpMessagingSystemFactory(200, 600000);
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(anUnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 500;
        }
    }
}

#endif