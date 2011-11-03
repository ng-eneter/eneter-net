/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

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
        /// Serializes the given data with using the compression.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                // Serialize data with the underlying serializer.
                object aSerializedData = myUnderlyingSerializer.Serialize<_T>(dataToSerialize);

                // Buffer the serialized data before the comprimation.
                // Note: The comprimation result is then much better -> if the serialized data is string.
                //       The problem probably is that the when a string is writen to the stream via the BinaryWriter
                //       the BinaryWriter writes it byte by byte. And if the stream would be GZipStream, the comprimation
                //       would not be so good as if all data is writen.
                byte[] aBufferedData;
                if (aSerializedData is byte[])
                {
                    aBufferedData = aSerializedData as byte[];
                }
                else if (aSerializedData is string)
                {
                    using (MemoryStream aBufStream = new MemoryStream())
                    {
                        using (BinaryWriter aBinaryWitier = new BinaryWriter(aBufStream))
                        {
                            aBinaryWitier.Write(aSerializedData as string);
                        }

                        aBufferedData = aBufStream.ToArray();
                    }
                }
                else
                {
                    string aMessage = TracedObject + "failed to compress data because the underlying serialier does not serialize data to string or to byte[].";
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                // Use memory stream to store the compressed data.
                using (MemoryStream aCompressedData = new MemoryStream())
                {
                    // Create GZipStream to compress data.
                    using (GZipStream aGzipStream = new GZipStream(aCompressedData, CompressionMode.Compress))
                    {
                        aGzipStream.Write(aBufferedData, 0, aBufferedData.Length);
                    }

                    byte[] aCompressedDataArray = aCompressedData.ToArray();

                    if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                    {
                        EneterTrace.Debug("Size before compression: " + aBufferedData.Length.ToString() + " Compressed size: " + aCompressedDataArray.Length.ToString());
                    }

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
                byte[] aCompressedData = serializedData as byte[];

                object aDecompressedData = null;

                // Put compressed data to the stream.
                using (MemoryStream aCompressedStream = new MemoryStream(aCompressedData))
                {
                    // Create the GZipStream to decompress data.
                    using (GZipStream aGzipStream = new GZipStream(aCompressedStream, CompressionMode.Decompress))
                    {
                        if (myUnderlayingSerializerUsesStringFlag)
                        {
                            using (BinaryReader aBinaryReader = new BinaryReader(aGzipStream))
                            {
                                aDecompressedData = aBinaryReader.ReadString();
                            }
                        }
                        else
                        {
                            // Create the stream where decompressed bytes will be stored.
                            using (MemoryStream aDecompressedBytesStream = new MemoryStream())
                            {
                                // Decompress data.
                                int aSize = 0;
                                byte[] aBuf = new byte[32768];
                                while ((aSize = aGzipStream.Read(aBuf, 0, aBuf.Length)) != 0)
                                {
                                    aDecompressedBytesStream.Write(aBuf, 0, aSize);
                                }
                                aDecompressedBytesStream.Position = 0;

                                aDecompressedData = aDecompressedBytesStream.ToArray();
                            }
                        }

                    }
                }

                _T aDeserializedData = myUnderlyingSerializer.Deserialize<_T>(aDecompressedData);

                return aDeserializedData;
            }
        }

        private ISerializer myUnderlyingSerializer;
        private bool myUnderlayingSerializerUsesStringFlag;


        private string TracedObject { get { return "GZipSerializer "; } }
    }
}


#endif