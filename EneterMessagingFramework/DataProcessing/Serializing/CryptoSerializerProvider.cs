using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    internal class CryptoSerializerProvider : ISerializer
    {
        public enum CryptoType
        {
            Rijndael,
            AES
        }

        public CryptoSerializerProvider(CryptoType cryptoType, ISerializer underlyingSerializer, byte[] key, byte[] initializeVector)
        {
            using (EneterTrace.Entering())
            {
                myCryptoType = cryptoType;
                myUnderlyingSerializer = underlyingSerializer;

                mySecretKey = key;
                myInitializeVector = initializeVector;

                // The sample helps to recognize, if the underlying serializer returns string or byte[].
                object aSample = myUnderlyingSerializer.Serialize<byte>(0);
                if (!(aSample is string) && !(aSample is byte[]))
                {
                    string aMessage = TracedObject + "failed in the constructor because the underalying serializer does not serialize into byte[] or string.";
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                myUnderlayingSerializerUsesStringFlag = aSample is string;
            }
        }
        
        /// <summary>
        /// Serializes data to the object.
        /// The returned object is type of byte[]. Output bytes are encrypted.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                // Serialize data.
                object aSerializedData = myUnderlyingSerializer.Serialize<_T>(dataToSerialize);

                // The stream containing encrypted data.
                MemoryStream anEncryptedDataStream = null;

                // Object used to encrypt data.
                SymmetricAlgorithm anEncryptor = null;

                try
                {
                    if (myCryptoType == CryptoType.AES)
                    {
                        anEncryptor = new AesManaged();
                    }
#if !SILVERLIGHT
                    else
                    {
                        anEncryptor = new RijndaelManaged();
                    }
#endif

                    anEncryptor.Key = mySecretKey;
                    anEncryptor.IV = myInitializeVector;

                    // Object performing the encryption.
                    ICryptoTransform aTransformer = anEncryptor.CreateEncryptor(anEncryptor.Key, anEncryptor.IV);

                    // Encrypt data.
                    anEncryptedDataStream = new MemoryStream();
                    using (CryptoStream csEncrypt = new CryptoStream(anEncryptedDataStream, aTransformer, CryptoStreamMode.Write))
                    {
                        if (aSerializedData is string)
                        {
                            using (BinaryWriter aBinaryWitier = new BinaryWriter(csEncrypt))
                            {
                                aBinaryWitier.Write(aSerializedData as string);
                            }
                        }
                        else if (aSerializedData is byte[])
                        {
                            using (BinaryWriter aBinaryWitier = new BinaryWriter(csEncrypt))
                            {
                                aBinaryWitier.Write(aSerializedData as byte[]);
                            }
                        }
                        else
                        {
                            string aMessage = TracedObject + "failed to encrypt data because the underlying serialier does not serialize data to string or to byte[].";
                            EneterTrace.Error(aMessage);
                            throw new InvalidOperationException(aMessage);
                        }
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (anEncryptor != null)
                    {
                        anEncryptor.Clear();
                    }
                }

                return anEncryptedDataStream.ToArray();
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
                byte[] anEncryptedData = serializedData as byte[];

                // Object used to decrypt data.
                SymmetricAlgorithm aDecryptor = null;

                object aDecodedData = null;

                try
                {
                    // Create a RijndaelManaged object
                    // with the specified key and IV.
                    if (myCryptoType == CryptoType.AES)
                    {
                        aDecryptor = new AesManaged();
                    }
#if !SILVERLIGHT
                    else
                    {
                        aDecryptor = new RijndaelManaged();
                    }
#endif
                    aDecryptor.Key = mySecretKey;
                    aDecryptor.IV = myInitializeVector;

                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform aTransfromer = aDecryptor.CreateDecryptor(aDecryptor.Key, aDecryptor.IV);

                    // Create the streams used for decryption.
                    using (MemoryStream anEncryptedStream = new MemoryStream(anEncryptedData))
                    {
                        using (CryptoStream aCryptoStream = new CryptoStream(anEncryptedStream, aTransfromer, CryptoStreamMode.Read))
                        {
                            if (myUnderlayingSerializerUsesStringFlag)
                            {
                                using (BinaryReader aBinaryReader = new BinaryReader(aCryptoStream))
                                {
                                    aDecodedData = aBinaryReader.ReadString();
                                }
                            }
                            else
                            {
                                // Create the stream where decoded bytes will be stored.
                                using (MemoryStream aDecodedBytesStream = new MemoryStream())
                                {
                                    // Decompress data.
                                    int aSize = 0;
                                    byte[] aBuf = new byte[32764];
                                    while ((aSize = aCryptoStream.Read(aBuf, 0, aBuf.Length)) != 0)
                                    {
                                        aDecodedBytesStream.Write(aBuf, 0, aSize);
                                    }
                                    aDecodedBytesStream.Position = 0;

                                    aDecodedData = aDecodedBytesStream.ToArray();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aDecryptor != null)
                    {
                        aDecryptor.Clear();
                    }
                }

                // Deserialize decrypted data.
                return myUnderlyingSerializer.Deserialize<_T>(aDecodedData);
            }
        }

        private CryptoType myCryptoType;
        private ISerializer myUnderlyingSerializer;
        private byte[] mySecretKey;
        private byte[] myInitializeVector;

        private bool myUnderlayingSerializerUsesStringFlag;

        private string TracedObject { get { return "CryptoSerializerProvider "; } }
    }
}
