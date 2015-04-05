using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;
using System.IO;

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
            UnderlyingMessaging = new SynchronousMessagingSystemFactory();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMessagingFactory(UnderlyingMessaging, aMaxOfflineTime);
            ConnectionInterruptionFrequency = 5;
        }


        //public override void A08_ResponseReceiverReconnects_AfterStopListening()
        //{
        //    // Not applicable
        //    // The problem is that for synchronous local messaging it is not possible to detect the input channel stoppped to listen.
        //    // E.g. it is possible to detect when using TCP.
        //}
    }
}
