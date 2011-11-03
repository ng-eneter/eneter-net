﻿
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencedRequestResponse_SynchronousMessaging_BinarySerializer : TypedSequencedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            string aChannelId = "Channel1";
            ISerializer aSerializer = new BinarySerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}

#endif