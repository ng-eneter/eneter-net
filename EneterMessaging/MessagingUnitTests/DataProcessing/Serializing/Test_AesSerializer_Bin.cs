
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_AesSerializer_Bin : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new AesSerializer("mytestpassword", new BinarySerializer());
            TestedSerializer2 = new AesSerializer("mytestpassword", new BinarySerializer());
        }
    }
}