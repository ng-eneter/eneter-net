/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.IO;
using System.Security.Cryptography;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    internal class CryptoSerializerProvider
    {
        public CryptoSerializerProvider(ISerializer underlyingSerializer, DeriveBytes passwordBasedKeyGenerator, int keyBitSize)
            : this(underlyingSerializer, passwordBasedKeyGenerator.GetBytes(keyBitSize / 8), passwordBasedKeyGenerator.GetBytes(16))
        {
        }

        public CryptoSerializerProvider(ISerializer underlyingSerializer, byte[] key, byte[] iv)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingSerializer = underlyingSerializer;

                mySecretKey = key;
                myInitializeVector = iv;
            }
        }
        
        /// <summary>
        /// Serializes data to the object.
        /// The returned object is type of byte[]. Output bytes are encrypted.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <param name="algorithm">algorithm used to encrypt the serialized data</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize, SymmetricAlgorithm algorithm)
        {
            using (EneterTrace.Entering())
            {
                // Use memory stream to store the compressed data.
                using (MemoryStream anEncryptedData = new MemoryStream())
                {
                    // Algorythm encrypting data.
                    SymmetricAlgorithm anAlgorithm = algorithm;
                    anAlgorithm.Key = mySecretKey;
                    anAlgorithm.IV = myInitializeVector;

                    // Object performing the encryption.
                    ICryptoTransform aTransformer = anAlgorithm.CreateEncryptor(anAlgorithm.Key, anAlgorithm.IV);

                    // Create stream encrypting data.
                    using (CryptoStream anEncryptor = new CryptoStream(anEncryptedData, aTransformer, CryptoStreamMode.Write))
                    {
                        // Use underlying serializer to serialize data.
                        object aSerializedData = myUnderlyingSerializer.Serialize<_T>(dataToSerialize);
                        myEncoderDecoder.Encode(anEncryptor, aSerializedData);
                    }

                    byte[] aCompressedDataArray = anEncryptedData.ToArray();
                    return aCompressedDataArray;
                }
            }
        }

        /// <summary>
        /// Deserializes data into the specified type.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Encrypted data to be deserialized.</param>
        /// <param name="algorithm">algorithm used to decrypt data before deserialization</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData, SymmetricAlgorithm algorithm)
        {
            using (EneterTrace.Entering())
            {
                byte[] anEncryptedData = (byte[])serializedData;

                // Put encrypted data to the stream.
                using (MemoryStream anEncryptedDataStream = new MemoryStream(anEncryptedData))
                {
                    // Algorythm decrypting data.
                    SymmetricAlgorithm aDecryptor = algorithm;
                    aDecryptor.Key = mySecretKey;
                    aDecryptor.IV = myInitializeVector;

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform aTransfromer = aDecryptor.CreateDecryptor(aDecryptor.Key, aDecryptor.IV);

                    using (CryptoStream aCryptoStream = new CryptoStream(anEncryptedDataStream, aTransfromer, CryptoStreamMode.Read))
                    {
                        object aDecodedData = myEncoderDecoder.Decode(aCryptoStream);
                        _T aDeserializedData = myUnderlyingSerializer.Deserialize<_T>(aDecodedData);
                        return aDeserializedData;
                    }
                }
            }
        }

        private byte[] mySecretKey;
        private byte[] myInitializeVector;

        private ISerializer myUnderlyingSerializer;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
