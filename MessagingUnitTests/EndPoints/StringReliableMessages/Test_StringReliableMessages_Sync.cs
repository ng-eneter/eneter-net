using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.StringReliableMessages
{
    [TestFixture]
    public class Test_StringReliableMessages_Sync : StringReliableMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystem = new ReliableMessagingFactory(anUnderlyingMessaging);
            string aChannelId = "Channel1";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}
