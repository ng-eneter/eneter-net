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
    public class Test_RsaSerializer_Xml : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            RSACryptoServiceProvider aCryptoProvider = new RSACryptoServiceProvider();
            RSAParameters aPublicKey = aCryptoProvider.ExportParameters(false);
            RSAParameters aPrivateKey = aCryptoProvider.ExportParameters(true);

            TestedSerializer = new RsaSerializer(aPublicKey, aPrivateKey);
            TestedSerializer2 = new RsaSerializer(aPublicKey, aPrivateKey);
        }
    }
}
