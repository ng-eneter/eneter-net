using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ThreadMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.StringMessages
{
    [TestFixture]
    public class Test_StringMessages_ThreadMessaging : StringMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new ThreadMessagingSystemFactory();
            string aChannelId = "Channel1";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}
