using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Security.Cryptography;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_RsaSerializer_Bin : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            RSACryptoServiceProvider aCryptoProvider = new RSACryptoServiceProvider();
            RSAParameters aPublicKey = aCryptoProvider.ExportParameters(false);
            RSAParameters aPrivateKey = aCryptoProvider.ExportParameters(true);

            TestedSerializer = new RsaSerializer(aPublicKey, aPrivateKey, 128, new BinarySerializer());
            TestedSerializer2 = new RsaSerializer(aPublicKey, aPrivateKey, 128, new BinarySerializer());
        }
    }
}
