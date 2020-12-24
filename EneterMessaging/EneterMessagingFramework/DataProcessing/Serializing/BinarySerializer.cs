

using Eneter.Messaging.Diagnostic;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializes data into .NET specific byte sequence.
    /// </summary>
    /// <remarks>
    /// The serializer internally uses BinaryFormatter provided by .Net.
    /// The data is serialized in the binary format.
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
        // Helper binder always binding deserialized data with the specified type.
        // It is used in case the enforce deserialization binding is enabled.
        private class Binder<T> : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// Constructs the binary serializer.
        /// </summary>
        public BinarySerializer()
            : this(false)
        {
        }

        /// <summary>
        /// Constructs the binary serializer with a possibility to enforce deserialization into desired type.
        /// </summary>
        /// <remarks>
        /// If the input parameter is false then if the assembly and the namespace of deserialized type does not match with
        /// the assembly and the namespace of serialized data the exception is thrown.
        /// If the input parameter is true then the deserialized type does not have to be from the same assembly and the namespace.<br/>
        /// The defaulat setting is false.
        /// </remarks>
        /// <param name="enforceTypeBinding"></param>
        public BinarySerializer(bool enforceTypeBinding)
        {
            using (EneterTrace.Entering())
            {
                myEnforceTypeBinding = enforceTypeBinding;
            }
        }

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
                        // Note: BinaryFormatter is not able to serialize null.
                        if (dataToSerialize == null)
                        {
                            return myNull;
                        }

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

                if (aData.Length == 0)
                {
                    // It will return null.
                    return default(_T);
                }

                try
                {
                    using (MemoryStream aMemoryStream = new MemoryStream(aData))
                    {
                        BinaryFormatter aBinaryFormatter = new BinaryFormatter();
                        if (myEnforceTypeBinding)
                        {
                            aBinaryFormatter.Binder = new Binder<_T>();
                        }
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


        private bool myEnforceTypeBinding;

        private static byte[] myNull = { };
    }
}
