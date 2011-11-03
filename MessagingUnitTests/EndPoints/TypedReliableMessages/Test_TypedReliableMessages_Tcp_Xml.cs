﻿

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedReliableMessages
{
    [TestFixture]
    public class Test_TypedReliableMessages_Tcp_Xml : TypedReliableMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ISerializer aSerializer = new XmlStringSerializer();
            IMessagingSystemFactory anUnderlyingMessagingSystem = new TcpMessagingSystemFactory();
            IReliableMessagingFactory aReliableMessaging = new ReliableMessagingFactory(anUnderlyingMessagingSystem);

            string aChannelId = "tcp://127.0.0.1:6765/";

            Setup(aReliableMessaging, aChannelId, aSerializer);
        }
    }
}

#endif