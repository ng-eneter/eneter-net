using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_DataContractXmlStringSerializer : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new DataContractXmlStringSerializer();
            TestedSerializer2 = new DataContractXmlStringSerializer();
        }
    }
}
