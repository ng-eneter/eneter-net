
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.StringRequestResponse
{
    [TestFixture]
    public class Test_StringRequestResponse_NamedPipeMessaging : StringRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new NamedPipeMessagingSystemFactory();
            string aChannelId = "net.pipe://127.0.0.1/Channel1/";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}