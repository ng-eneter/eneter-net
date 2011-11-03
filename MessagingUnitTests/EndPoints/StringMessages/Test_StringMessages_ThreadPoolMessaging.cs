using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ThreadPoolMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.EndPoints.StringMessages
{
    [TestFixture]
    public class Test_StringMessages_ThreadPoolMessaging : StringMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new ThreadPoolMessagingSystemFactory();
            string aChannelId = "Channel1";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}
