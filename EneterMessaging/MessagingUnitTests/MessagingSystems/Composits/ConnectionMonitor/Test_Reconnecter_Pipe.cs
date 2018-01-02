#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_Reconnecter_Pipe : ReconnecterBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "net.pipe://127.0.0.1/Channel1/";
            MessagingSystemFactory = new NamedPipeMessagingSystemFactory(5, 500);
        }
    }
}

#endif