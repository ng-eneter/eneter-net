using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using System.Security.Cryptography;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_AesSerializer_Xml : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            TestedSerializer = new AesSerializer("mytestpassword", new XmlStringSerializer());
            TestedSerializer2 = new AesSerializer("mytestpassword", new XmlStringSerializer());
        }

        [Test]
        [ExpectedException(typeof(CryptographicException))]
        public void IncorrectPassword()
        {
            string aData = "Hello world.";

            // Serialize.
            object aSerializedData = TestedSerializer.Serialize<string>(aData);

            // Serializer with incorrect password.
            ISerializer anIncorrectSerializer = new AesSerializer("mytestpassword1");

            // Try to deserialize.
            string aDeserializedData = anIncorrectSerializer.Deserialize<string>(aSerializedData);
        }
    }
}
