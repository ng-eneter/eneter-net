﻿
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencedRequestResponse_NamedPipeMessaging_XmlSerializer : TypedSequencedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new NamedPipeMessagingSystemFactory();
            string aChannelId = "net.pipe://127.0.0.1/Channel1/";
            ISerializer aSerializer = new XmlStringSerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}

#endif