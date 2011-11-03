

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.StringMessages
{
    [TestFixture]
    public class Test_StringMessages_TcpMessaging : StringMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory();
            string aChannelId = "tcp://127.0.0.1:8090/";

            Setup(aMessagingSystem, aChannelId);
        }
    }
}

#endif