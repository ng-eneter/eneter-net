

using System.Security.Cryptography;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializer using AES (Advanced Encryption Standard).
    /// </summary>
    /// <remarks>
    /// The serializer uses an underlying serializer to serialize and deserialize data.
    /// Data encoded by the underlying serializer is then encrypted by AES.
    /// <example>
    /// Encrypted serialization with <see cref="XmlStringSerializer"/>
    /// <code>
    /// // Create the serializer. The defualt constructor uses XmlStringSerializer.
    /// AesSerializer aSerializer = new AesSerializer("My password.");
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
    public class AesSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer. It uses <see cref="XmlStringSerializer"/> as the underlying serializer.
        /// </summary>
        /// <param name="password">password used to generate 128 bit key</param>
        public AesSerializer(string password)
            : this(new XmlStringSerializer(), new Rfc2898DeriveBytes(password, new byte[] { 1, 3, 5, 8, 15, 254, 9, 189, 43, 129 }), 128)
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="password">password used to generate 128 bit key</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        public AesSerializer(string password, ISerializer underlyingSerializer)
            : this(underlyingSerializer, new Rfc2898DeriveBytes(password, new byte[] { 1, 3, 5, 8, 15, 254, 9, 189, 43, 129 }), 128)
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="password">password used to generate 128 bit key</param>
        /// <param name="salt">additional value used to calculate the key</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        public AesSerializer(string password, byte[] salt, ISerializer underlyingSerializer)
            : this(underlyingSerializer, new Rfc2898DeriveBytes(password, salt), 128)
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="password">password used to generate the key</param>
        /// <param name="salt">additional value used to calculate the key</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        /// <param name="keyBitSize">bit size of the key e.g. 128, 256</param>
        public AesSerializer(string password, byte[] salt, ISerializer underlyingSerializer, int keyBitSize)
            : this(underlyingSerializer, new Rfc2898DeriveBytes(password, salt), keyBitSize)
        {
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        /// <param name="passwordBasedKeyGenerator">generator of key from the password</param>
        /// <param name="keyBitSize">bit size of the key</param>
        public AesSerializer(ISerializer underlyingSerializer, DeriveBytes passwordBasedKeyGenerator, int keyBitSize)
        {
            using (EneterTrace.Entering())
            {
                myCryptoSerializer = new CryptoSerializerProvider(underlyingSerializer, passwordBasedKeyGenerator, keyBitSize);
            }
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="iv">initialization vector</param>
        /// <param name="underlyingSerializer">underlying serializer (e.g. XmlStringSerializer or BinarySerializer)</param>
        public AesSerializer(byte[] key, byte[] iv, ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                myCryptoSerializer = new CryptoSerializerProvider(underlyingSerializer, key, iv);
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
                return myCryptoSerializer.Serialize<_T>(dataToSerialize, new AesManaged());
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
                _T aDeserializedData = myCryptoSerializer.Deserialize<_T>(serializedData, new AesManaged());
                return aDeserializedData;
            }
        }


        private CryptoSerializerProvider myCryptoSerializer;
    }
}
