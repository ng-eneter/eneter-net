using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;
using System.IO;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_BufferedMessaging_Sync : BufferedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            ChannelId = "Channel_1";
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(anUnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 5;
        }
    }
}
