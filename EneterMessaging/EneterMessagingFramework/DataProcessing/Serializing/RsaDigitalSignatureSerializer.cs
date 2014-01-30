/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializer digitaly signing data.
    /// </summary>
    /// <remarks>
    /// Serialization:
    /// <ol>
    /// <li>Incoming data is serialized by underlying serializer (e.g. XmlStringSerializer)</li>
    /// <li>SHA1 hash is calculated from the serialized data.</li>
    /// <li>The hash is encrypted with RSA using the private key.</li>
    /// <li>The serialized data consists of serialized data, encoded hash (signature) and public certificate of the signer.</li>
    /// </ol>
    /// Deserialization:
    /// <ol>
    /// <li>The public certificate is taken from serialized data and verified. (you can provide your own verification)</li>
    /// <li>SHA1 hash is calculated from serialized data.</li>
    /// <li>Encrypted hash (signature) is decrypted by public key taken from the certificate.</li>
    /// <li>If the decrypted hash is same as calculated one the data is ok.</li>
    /// <li>Data is deserialized by the underlying serializer and returned.</li>
    /// </ol>
    /// <example>
    /// Example shows how to serialize/deserialize data using digital signature.
    /// <code>
    /// // Get the certificate containing public and private keys. E.g. from the file.
    /// X509Certificate2 aSignerCertificate = new X509Certificate2("c:/MyCertificate.pfx", "mypassword");
    /// 
    /// // Create the serializer.
    /// RsaDigitalSignatureSerializer aSerializer = new RsaDigitalSignatureSerializer(aSignerCertificate);
    /// 
    /// // Serialize data.
    /// // (serialized data will contain digital signature and signer's public certificate)
    /// object aSerializedData = aSerializer.Serialize&lt;string&gt;("Hello world.");
    /// 
    /// // Deserialize data.
    /// string aDeserializedData = aSerializer.Deserialize&lt;string&gt;(aSerializedData);
    /// </code>
    /// </example>
    /// </remarks>
    public class RsaDigitalSignatureSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer with default parameters.
        /// </summary>
        /// <remarks>
        /// It uses XmlStringSerializer as the underlying serializer and it uses default X509Certificate2.Verify() method to verify
        /// the public certificate.
        /// </remarks>
        /// <param name="signerCertificate">signer certificate containing public and also private part.
        /// The key from the private part is used to sign data during the serialization.<br/>
        /// The public certificate is attached to serialized data.<br/>
        /// If the parameter signerCertificate is null then the serializer can be used only for deserialization.
        /// </param>
        public RsaDigitalSignatureSerializer(X509Certificate2 signerCertificate)
            : this(signerCertificate, null, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the serializer with custom parameters.
        /// </summary>
        /// <param name="signerCertificate">signer certificate containing public and also private part.
        /// The key from the private part is used to sign data during the serialization.<br/>
        /// The public certificate is attached to serialized data.<br/>
        /// If the parameter certificate is null then the serializer can be used only for deserialization.
        /// </param>
        /// <param name="verifySignerCertificate">callback to verify the certificate. If null then default X509Certificate2.Verify() is used.</param>
        /// <param name="underlyingSerializer">underlying serializer that will be used to serialize/deserialize data</param>
        public RsaDigitalSignatureSerializer(X509Certificate2 signerCertificate, Func<X509Certificate2, bool> verifySignerCertificate, ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                // If the serializer shall be used for serialization too.
                if (signerCertificate != null && signerCertificate.PrivateKey as RSACryptoServiceProvider == null)
                {
                    throw new ArgumentException("The input parameter X509Certificate2 does not contain the private key of type RSACryptoServiceProvider.");
                }
                mySignerCertificate = signerCertificate;

                myPublicCertificate = signerCertificate.Export(X509ContentType.Cert);

                myVerifySignerCertificate = (verifySignerCertificate == null) ? VerifySignerCertificate : verifySignerCertificate;
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
                if (mySignerCertificate == null)
                {
                    throw new InvalidOperationException(TracedObject + "failed to serialize data. The signer certificate is null and thus the serializer can be used only for deserialization.");
                }

                byte[][] aSignedData = new byte[3][];

                using (MemoryStream aSerializedDataStream = new MemoryStream())
                {
                    // Serialize incoming data using underlying serializer.
                    object aSerializedData = myUnderlyingSerializer.Serialize<_T>(dataToSerialize);
                    myEncoderDecoder.Encode(aSerializedDataStream, aSerializedData);

                    aSignedData[0] = aSerializedDataStream.ToArray();
                }

                // Calculate hash from serialized data.
                SHA1Managed aSha1 = new SHA1Managed();
                byte[] aHash = aSha1.ComputeHash(aSignedData[0]);

                // Sign the hash.
                // Note: The signature is the hash encrypted with the private key.
                RSACryptoServiceProvider aPrivateKey = (RSACryptoServiceProvider)mySignerCertificate.PrivateKey;
                aSignedData[2] = aPrivateKey.SignHash(aHash, CryptoConfig.MapNameToOID("SHA1"));

                // Store the public certificate.
                aSignedData[1] = myPublicCertificate;

                // Serialize data together with the signature.
                object aSerializedSignedData = myUnderlyingSerializer.Serialize<byte[][]>(aSignedData);
                return aSerializedSignedData;
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
                // Deserialize data containing the signature.
                byte[][] aSignedData = myUnderlyingSerializer.Deserialize<byte[][]>(serializedData);

                // Verify the signer certificate. If we trust the certificate.
                X509Certificate2 aCertificate = new X509Certificate2(aSignedData[1]);
                if (!myVerifySignerCertificate(aCertificate))
                {
                    throw new InvalidOperationException(TracedObject + "failed to deserialize data because the verification of signer certificate failed.");
                }

                // Calculate the hash.
                SHA1Managed aSha1 = new SHA1Managed();
                byte[] aHash = aSha1.ComputeHash(aSignedData[0]);

                // Verify the signature.
                RSACryptoServiceProvider aCryptoServiceProvider = (RSACryptoServiceProvider)aCertificate.PublicKey.Key;
                if (!aCryptoServiceProvider.VerifyHash(aHash, CryptoConfig.MapNameToOID("SHA1"), aSignedData[2]))
                {
                    throw new InvalidOperationException(TracedObject + "failed to deserialize data because the signature verification failed.");
                }

                using (MemoryStream aDeserializedDataStream = new MemoryStream(aSignedData[0], 0, aSignedData[0].Length))
                {
                    object aDecodedData = myEncoderDecoder.Decode(aDeserializedDataStream);
                    _T aDeserializedData = myUnderlyingSerializer.Deserialize<_T>(aDecodedData);
                    return aDeserializedData;
                }
            }
        }

        private bool VerifySignerCertificate(X509Certificate2 signerCertificate)
        {
            return signerCertificate.Verify();
        }

        private ISerializer myUnderlyingSerializer;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
        private byte[] myPublicCertificate;
        private X509Certificate2 mySignerCertificate;
        private Func<X509Certificate2, bool> myVerifySignerCertificate;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}


#endif