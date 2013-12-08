using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_Reconnecter_Sync : ReconnecterBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "Channel1";
            MessagingSystemFactory = new SynchronousMessagingSystemFactory();
        }
    }
}
