#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_BufferedMessaging_Tcp : BufferedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "tcp://127.0.0.1:6070/";
            UnderlyingMessaging = new TcpMessagingSystemFactory();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(UnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 80;
        }
    }
}

#endif