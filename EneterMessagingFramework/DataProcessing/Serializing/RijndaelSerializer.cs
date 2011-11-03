/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Security.Cryptography;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializer using Rijndael encryption.
    /// </summary>
    /// <remarks>
    /// The serializer internally uses some other serializer to serialize and deserialize data.
    /// Then it uses Rijndael to encrypt and decrypt the data.
    /// <br/><br/>
    /// Notice, the Rijndael encryption is not available in Silverlight platform.
    /// <example>
    /// Encrypted serialization with <see cref="XmlStringSerializer"/>
    /// <code>
    /// // Create the serializer. The defualt constructor uses XmlStringSerializer.
    /// RijndaelSerializer aSerializer = new RijndaelSerializer("My password.");
    /// 
    /// // Create some data to be serialized.
    /// MyData aData = new MyData();
    /// ...
    /// 
    /// // Serialize data with using AES.
    /// object aSerializedData = aSerializer.Serialize&lt;MyData&gt;(aData);
    /// </code>
    /// </example>
    /// </remarks>
    public class RijndaelSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer. It uses <see cref="XmlStringSerializer"/> as the underlying serializer.
        /// </summary>
        /// <param name="password">password used to generate 256 bit key</param>
        public RijndaelSerializer(string password)
            : this(password, new byte[] { 1, 80, 5, 10, 15, 254, 9, 18, 43, 180 }, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="password">password used to generate 256 bit key</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        public RijndaelSerializer(string password, ISerializer underlyingSerializer)
            : this(password, new byte[] { 1, 80, 5, 10, 15, 254, 9, 18, 43, 180 }, underlyingSerializer)
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="password">password used to generate 256 bit key</param>
        /// <param name="salt">additional value used to calculate the key</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        public RijndaelSerializer(string password, byte[] salt, ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                PasswordDeriveBytes aKeyGenerator = new PasswordDeriveBytes(password, salt);

                // Create 256bit key from the password and the initialization vector.
                byte[] aKey = aKeyGenerator.GetBytes(256 / 8);
                byte[] anInitializeVector = aKeyGenerator.GetBytes(16);

                myCryptoSerializer = new CryptoSerializerProvider(CryptoSerializerProvider.CryptoType.Rijndael, underlyingSerializer, aKey, anInitializeVector);
            }
        }
        
        /// <summary>
        /// Serializes data to the object.
        /// The returned object is type of byte[]. Returned bytes are encrypted.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                return myCryptoSerializer.Serialize<_T>(dataToSerialize);
            }
        }

        /// <summary>
        /// Deserializes data into the specified type.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Encrypted data to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                return myCryptoSerializer.Deserialize<_T>(serializedData);
            }
        }


        private CryptoSerializerProvider myCryptoSerializer;
    }
}

#endif