/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT && !COMPACT_FRAMEWORK20

using System;
using System.IO;
using System.IO.Compression;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializer compressing and decompressing data.
    /// </summary>
    /// <remarks>
    /// The serializer internally uses GZipStream to compress and decompress data.
    /// <example>
    /// Example shows how to serialize data.
    /// <code>
    /// // Creat the serializer.
    /// GZipSerializer aSerializer = new GZipSerializer();
    /// 
    /// // Create some data to be serialized.
    /// MyData aData = new MyData();
    /// ...
    /// 
    /// // Serialize data. Serialized data will be compressed.
    /// object aSerializedData = aSerializer.Serialize&lt;MyData&gt;(aData);
    /// </code>
    /// </example>
    /// </remarks>
    public class GZipSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer with XmlStringSerializer as the underlying serializer.
        /// </summary>
        /// <remarks>
        /// The serializer uses the underlying serializer to serialize data before the compression.
        /// It also uses the underlying serializer to deserialize decompressed data.
        /// </remarks>
        public GZipSerializer()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the serializer with the given underlying serializer.
        /// </summary>
        /// <remarks>
        /// The serializer uses the underlying serializer to serialize data before the compression.
        /// It also uses the underlying serializer to deserialize decompressed data.
        /// </remarks>
        /// <param name="underlyingSerializer">underlying serializer</param>
        public GZipSerializer(ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingSerializer = underlyingSerializer;
            }
        }

        /// <summary>
        /// Serializes the given data with using the compression.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                // Use memory stream to store the compressed data.
                using (MemoryStream aCompressedData = new MemoryStream())
                {
                    // Create GZipStream to compress data.
                    using (GZipStream aGzipStream = new GZipStream(aCompressedData, CompressionMode.Compress))
                    {
                        // Use underlying serializer to serialize data.
                        object aSerializedData = myUnderlyingSerializer.Serialize<_T>(dataToSerialize);
                        myEncoderDecoder.Encode(aGzipStream, aSerializedData);
                    }

                    byte[] aCompressedDataArray = aCompressedData.ToArray();

                    return aCompressedDataArray;
                }
            }
        }

        /// <summary>
        /// Deserializes compressed data into the specified type.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Compressed data to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                byte[] aCompressedData = (byte[])serializedData;

                // Put compressed data to the stream.
                using (MemoryStream aCompressedStream = new MemoryStream(aCompressedData))
                {
                    // Create the GZipStream to decompress data.
                    using (GZipStream aGzipStream = new GZipStream(aCompressedStream, CompressionMode.Decompress))
                    {
                        object aDecodedData = myEncoderDecoder.Decode(aGzipStream);
                        _T aDeserializedData = myUnderlyingSerializer.Deserialize<_T>(aDecodedData);
                        return aDeserializedData;
                    }
                }
            }
        }

        private ISerializer myUnderlyingSerializer;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();


        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}


#endif