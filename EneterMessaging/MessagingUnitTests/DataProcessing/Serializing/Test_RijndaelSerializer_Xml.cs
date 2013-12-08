
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
    public class Test_RijndaelSerializer_Xml : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new RijndaelSerializer("mytestpassword", new XmlStringSerializer());
            TestedSerializer2 = new RijndaelSerializer("mytestpassword", new XmlStringSerializer());
        }
    }
}

#endif