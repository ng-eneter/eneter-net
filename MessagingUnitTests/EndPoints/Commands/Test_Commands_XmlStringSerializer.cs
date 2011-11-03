﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.Commands
{
    [TestFixture]
    public class Test_Commands_XmlStringSerializer : CommandTesterBase
    {
        [SetUp]
        public void Setup()
        {
            Serializer = new XmlStringSerializer();
        }
    }
}
