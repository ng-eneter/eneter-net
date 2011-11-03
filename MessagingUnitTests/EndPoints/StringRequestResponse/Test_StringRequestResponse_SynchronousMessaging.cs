using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.StringRequestResponse
{
    [TestFixture]
    public class Test_StringRequestResponse_SynchronousMessaging : StringRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            string aChannelId = "Channel1";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}
