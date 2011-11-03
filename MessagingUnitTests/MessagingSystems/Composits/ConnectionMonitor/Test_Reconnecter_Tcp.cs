#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_Reconnecter_Tcp : ReconnecterBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "tcp://127.0.0.1:5060/";
            MessagingSystemFactory = new TcpMessagingSystemFactory();
        }
    }
}

#endif