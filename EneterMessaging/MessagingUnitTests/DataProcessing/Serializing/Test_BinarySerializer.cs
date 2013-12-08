#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_BinarySerializer : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new BinarySerializer();
            TestedSerializer2 = new BinarySerializer();
        }
    }
}

#endif //!SILVERLIGHT