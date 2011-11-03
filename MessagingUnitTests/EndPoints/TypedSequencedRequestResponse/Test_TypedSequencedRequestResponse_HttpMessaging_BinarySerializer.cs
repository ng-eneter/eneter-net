
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencedRequestResponse_HttpMessaging_BinarySerializer : TypedSequencedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new HttpMessagingSystemFactory();
            string aChannelId = "http://127.0.0.1:8090/";
            ISerializer aSerializer = new BinarySerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}

#endif