
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_DataContractJsonStringSerializer : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new DataContractJsonStringSerializer();
            TestedSerializer2 = new DataContractJsonStringSerializer();
        }
    }
}

#endif