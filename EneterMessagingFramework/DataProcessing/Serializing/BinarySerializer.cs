/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Implements the serialization/deserialization to/from sequence of bytes.
    /// </summary>
    /// <remarks>
    /// The serializer internally uses BinaryFormatter provided by .Net.
    /// The data is serialized in the binary format.
    /// <br/><br/>
    /// This serializer is not supported in Silverlight and Windows Phone 7 platforms.
    /// <br/>
    /// <example>
    /// Serialization and deserialization example.
    /// <code>
    /// // Some class to be serialized.
    /// [Serializable]
    /// public class MyClass
    /// {
    ///     public int Value1 { get; set; }
    ///     public string Value2 { get; set; }
    /// }
    /// 
    /// ...
    /// 
    /// MyClass c = new MyClass;
    /// c.Value1 = 10;
    /// c.Value2 = "Hello World.";
    /// 
    /// ...
    /// 
    /// // Serialization
    /// BinarySerializer aSerializer = new BinarySerializer();
    /// object aSerializedObject = aSerializer.Serialize&lt;MyClass&gt;(c);
    /// 
    /// ...
    /// 
    /// // Deserialization
    /// BinarySerializer aSerializer = new BinarySerializer();
    /// object aDeserializedObject = aSerializer.Deserialize&lt;MyClass&gt;(aSerializedObject);
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public class BinarySerializer : ISerializer
    {
        /// <summary>
        /// Serializes data to the sequnce of bytes, byte[].
        /// </summary>
        /// <remarks>
        /// It internally BinaryFormatter provided by .Net.
        /// </remarks>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>Data serialized in byte[].</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (MemoryStream aMemoryStream = new MemoryStream())
                    {
                        BinaryFormatter aBinaryFormatter = new BinaryFormatter();
                        aBinaryFormatter.Serialize(aMemoryStream, dataToSerialize);
                        byte[] aSerializedData = aMemoryStream.ToArray();

                        return aSerializedData;
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The serialization to binary format failed.", err);
                    throw;
                }
            }
        }

        /// <summary>
        /// Deserializes data into the specified type.
        /// </summary>
        /// <remarks>
        /// It internally BinaryFormatter provided by .Net.
        /// </remarks>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">the sequence of bytes (byte[]), to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                byte[] aData = (byte[])serializedData;

                try
                {
                    using (MemoryStream aMemoryStream = new MemoryStream(aData))
                    {
                        BinaryFormatter aBinaryFormatter = new BinaryFormatter();
                        _T aDeserializedData = (_T)aBinaryFormatter.Deserialize(aMemoryStream);

                        return aDeserializedData;
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The deserialization from binary format failed.", err);
                    throw;
                }
            }
        }

    }
}

#endif //!SILVERLIGHT