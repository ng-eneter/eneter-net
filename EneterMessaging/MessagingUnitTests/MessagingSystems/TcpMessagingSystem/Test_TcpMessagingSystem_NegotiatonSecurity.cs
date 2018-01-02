
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_NegotiatonSecurity : Test_TcpMessagingSystem_Synchronous
    {
        [SetUp]
        public new void Setup()
        {
            MessagingSystemFactory = new TcpMessagingSystemFactory()
            {
                ClientSecurityStreamFactory = new ClientNegotiateFactory(),
                ServerSecurityStreamFactory = new ServerNegotiateFactory()
            };

            ChannelId = "tcp://127.0.0.1:8091/";
        }
    }
}