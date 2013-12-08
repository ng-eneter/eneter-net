/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System.Security.Cryptography;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializer using RSA.
    /// </summary>
    /// <remarks>
    /// The serialization:
    /// <ol>
    /// <li>Incoming data is serialized by underlying serializer (e.g. XmlStringSerializer)</li>
    /// <li>The random key is generated and used with AES algorythm to encrypt the serialized data.</li>
    /// <li>The random key for AES is encrypted by RSA using the public key.</li>
    /// <li>The serialized data consits of AES encrypted data and RSA encrypted key for AES.</li>
    /// </ol>
    /// The deserialization:
    /// <ol>
    /// <li>The receiver decrypts the AES key by RSA using its private key.</li>
    /// <li>Decrypted key is used to decrypt AES encrypted data.</li>
    /// <li>Decrypted data is deserialized by underlying serialized (e.g. XmlStringSerializer)</li>
    /// <li>The deserialization returns deserialized data.</li>
    /// </ol>
    /// <br/>
    /// <example>
    /// Example shows how to serialize/deserialize data.
    /// <code>
    /// // Generate public and private keys.
    /// RSACryptoServiceProvider aCryptoProvider = new RSACryptoServiceProvider();
    /// RSAParameters aPublicKey = aCryptoProvider.ExportParameters(false);
    /// RSAParameters aPrivateKey = aCryptoProvider.ExportParameters(true);
    /// 
    /// // Create the serializer.
    /// RsaSerializer aSerializer = new RsaSerializer(aPublicKey, aPrivateKey);
    /// 
    /// // Serialize data.
    /// object aSerializedData = aSerializer.Serialize&lt;string&gt;("Hello world.");
    /// 
    /// 
    /// // Deserialize data.
    /// string aDeserializedData = aSerializer.Deserialize&lt;string&gt;(aSerializedData);
    /// </code>
    /// </example>
    /// </remarks>
    public class RsaSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the RSA serializer with default paraneters.
        /// </summary>
        /// <remarks>
        /// It uses XmlStringSerializer and it will generate
        /// 128 bit key for the AES algorythm.
        /// </remarks>
        /// <param name="publicKey">public key used for serialization. If the serializer is used only for deserialization then you can provide
        /// a dummy key e.g. new RSAParameters().
        /// </param>
        /// <param name="privateKey">private key used for deserialization. If the serializer is used only for serialization then you can provide
        /// a dummy key e.g. new RSAParameters().
        /// </param>
        public RsaSerializer(RSAParameters publicKey, RSAParameters privateKey)
            : this(publicKey, privateKey, 128, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the RSA serializer with custom parameters.
        /// </summary>
        /// <param name="publicKey">public key used for serialization. If the serializer is used only for deserialization then you can provide
        /// a dummy key e.g. new RSAParameters().
        /// </param>
        /// <param name="privateKey">private key used for deserialization. If the serializer is used only for serialization then you can provide
        /// a dummy key e.g. new RSAParameters().
        /// </param>
        /// <param name="aesBitSize">size of the random key generated for the AES encryption, 128, 256, ...</param>
        /// <param name="underlyingSerializer">underlying serializer used to serialize/deserialize data e.g. XmlStringSerializer</param>
        public RsaSerializer(RSAParameters publicKey, RSAParameters privateKey, int aesBitSize, ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                myPublicKey = publicKey;
                myPrivateKey = privateKey;
                myAesBitSize = aesBitSize;
                myUnderlyingSerializer = underlyingSerializer;
            }
        }

        /// <summary>
        /// Serializes data.
        /// </summary>
        /// <typeparam name="_T">data type to be serialized</typeparam>
        /// <param name="dataToSerialize">data to be serialized</param>
        /// <returns>serialized data</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                byte[][] aData = new byte[3][];

                // Generate random key for AES.
                AesManaged anAes = new AesManaged();
                anAes.KeySize = myAesBitSize;
                anAes.GenerateIV();
                anAes.GenerateKey();

                // Serialize data with AES using the random key.
                AesSerializer anAesSerializer = new AesSerializer(anAes.Key, anAes.IV, myUnderlyingSerializer);
                aData[2] = (byte[])anAesSerializer.Serialize<_T>(dataToSerialize);

                // Encrypt the random key with RSA using the public key.
                // Note: Only guy having the private key can decrypt it.
                RSACryptoServiceProvider aCryptoServiceProvider = new RSACryptoServiceProvider();
                aCryptoServiceProvider.ImportParameters(myPublicKey);
                aData[0] = aCryptoServiceProvider.Encrypt(anAes.Key, false);
                aData[1] = aCryptoServiceProvider.Encrypt(anAes.IV, false);

                // Serialize encrypted data, key and iv with the underlying serializer.
                object aSerializedData = myUnderlyingSerializer.Serialize<byte[][]>(aData);

                return aSerializedData;
            }
        }

        /// <summary>
        /// Deserializes data.
        /// </summary>
        /// <typeparam name="_T">data type to be deserialized</typeparam>
        /// <param name="serializedData">serialized data</param>
        /// <returns>deserialized data type</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                // Deserialize data
                byte[][] aData = myUnderlyingSerializer.Deserialize<byte[][]>(serializedData);

                // Use the private key to decrypt the key and iv for the AES.
                RSACryptoServiceProvider aCryptoServiceProvider = new RSACryptoServiceProvider();
                aCryptoServiceProvider.ImportParameters(myPrivateKey);
                byte[] aKey = aCryptoServiceProvider.Decrypt(aData[0], false);
                byte[] anIv = aCryptoServiceProvider.Decrypt(aData[1], false);

                // Decrypt data content which its encrypted with AES.
                AesSerializer anAes = new AesSerializer(aKey, anIv, myUnderlyingSerializer);
                _T aDeserializedData = anAes.Deserialize<_T>(aData[2]);

                return aDeserializedData;
            }
        }

        private ISerializer myUnderlyingSerializer;
        private int myAesBitSize;
        private RSAParameters myPrivateKey;
        private RSAParameters myPublicKey;
    }
}


#endif