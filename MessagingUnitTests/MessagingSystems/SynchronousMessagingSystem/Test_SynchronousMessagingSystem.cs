using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using System.Threading;


namespace Eneter.MessagingUnitTests.MessagingSystems.SynchronousMessagingSystem
{
    [TestFixture]
    public class Test_SynchronousMessagingSystem : MessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            MessagingSystemFactory = new SynchronousMessagingSystemFactory();
        }
    }
}
