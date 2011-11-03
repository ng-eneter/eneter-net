
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using System.Net;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpMessagingSystem_Synchronous : HttpMessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            MessagingSystemFactory = new HttpMessagingSystemFactory();
            ChannelId = "http://127.0.0.1:8091/";
        }

        [Test]
        [ExpectedException(typeof(WebException))]
        public override void A07_StopListening()
        {
            base.A07_StopListening();
        }

    }
}

#endif