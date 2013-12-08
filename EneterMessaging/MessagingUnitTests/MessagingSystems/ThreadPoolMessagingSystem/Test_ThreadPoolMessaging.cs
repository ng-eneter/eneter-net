using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ThreadPoolMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.ThreadPoolMessagingSystem
{
    [TestFixture]
    public class Test_ThreadPoolMessaging : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            MessagingSystemFactory = new ThreadPoolMessagingSystemFactory();
        }
    }
}
