﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !MONO && !NET35 && !WINDOWS_PHONE_70 && !WINDOWS_PHONE_71 && !SILVERLIGHT3 && !SILVERLIGHT4 && !SILVERLIGHT5 && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Serializes data into JSON.
    /// </summary>
    /// <remarks>
    /// The serializer is based on DataContractJsonSerializer provided by .Net and it supports
    /// attributes (DataContract, DataMember, ...) specifying how data is serialized/deserialized.
    /// </remarks>
    public class DataContractJsonStringSerializer : ISerializer
    {
        /// <summary>
        /// Creates the serializer based on DataContractSerializer with default settings.
        /// </summary>
        /// <remarks>
        /// XmlDictionaryReaderQuotas are set to Max values.
        /// </remarks>
        public DataContractJsonStringSerializer()
            : this(XmlDictionaryReaderQuotas.Max, x => new DataContractJsonSerializer(x))
        {
        }

        /// <summary>
        /// Creates serializer based on DataContractJsonSerializer.
        /// </summary>
        /// <param name="quotas">various quotas vsalues for the deserialization.</param>
        public DataContractJsonStringSerializer(XmlDictionaryReaderQuotas quotas)
            : this(quotas, x => new DataContractJsonSerializer(x))
        {
        }

        /// <summary>
        /// Creates the serializer that allows user to specify his own method to instantiate DataContractJsonSerializer
        /// with desired settings.
        /// </summary>
        /// <param name="quotas">various quotas vsalues for the deserialization.</param>
        /// <param name="dataContractFactoryMethod">
        /// The factory method responsible for creation of DataContractJsonSerializer.
        /// The factory method is called during the serialization or deserialization.<br/>
        /// The factory method can be called from more threads at the same time, so be sure, your factory method
        /// is thread safe.
        /// </param>
#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
        public DataContractJsonStringSerializer(XmlDictionaryReaderQuotas quotas, Func<Type, XmlObjectSerializer> dataContractFactoryMethod)
#else
        public DataContractJsonStringSerializer(XmlDictionaryReaderQuotas quotas, Func<Type, DataContractJsonSerializer> dataContractFactoryMethod)
#endif

        {
            using (EneterTrace.Entering())
            {
                myDictionaryQuotas = quotas;
                myDataContractFactoryMethod = dataContractFactoryMethod;
            }
        }

        /// <summary>
        /// Serializes data to the JSON string.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>JSON string</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                string aSerializedObjectStr = "";

                try
                {
#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
                    using (MemoryStream aBuffer = new MemoryStream())
                    {
                        using (XmlWriter aJsonWriter = JsonReaderWriterFactory.CreateJsonWriter(aBuffer))
                        {
                            XmlObjectSerializer aSerializer = myDataContractFactoryMethod(typeof(_T));
                            aSerializer.WriteObject(aJsonWriter, dataToSerialize);

                            aJsonWriter.Flush();
                            aSerializedObjectStr = Encoding.UTF8.GetString(aBuffer.GetBuffer(), 0, (int)aBuffer.Length);
                        }
                    }
#else
                    using (MemoryStream aBuffer = new MemoryStream())
                    {
                        DataContractJsonSerializer aSerializer = myDataContractFactoryMethod(typeof(_T));
                        aSerializer.WriteObject(aBuffer, dataToSerialize);
                        aSerializedObjectStr = Encoding.UTF8.GetString(aBuffer.GetBuffer(), 0, (int)aBuffer.Length);
                    }
#endif
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The serialization to JSON failed.", err);
                    throw;
                }

                return aSerializedObjectStr;
            }
        }

        /// <summary>
        /// Deserializes JSON string into the specified type.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Data serialized in JSON string.</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                string aSerializedJson = (string)serializedData;

                try
                {
#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
                    byte[] aBuffer = Encoding.UTF8.GetBytes(aSerializedJson);
                    using (XmlReader anXmlReader = JsonReaderWriterFactory.CreateJsonReader(aBuffer, myDictionaryQuotas))
                    {
                        XmlObjectSerializer aSerializer = myDataContractFactoryMethod(typeof(_T));
                        return (_T)aSerializer.ReadObject(anXmlReader);
                    }
#else
                    byte[] aBuffer = Encoding.UTF8.GetBytes(aSerializedJson);
                    using (MemoryStream aStream = new MemoryStream(aBuffer))
                    {
                        DataContractJsonSerializer aSerializer = myDataContractFactoryMethod(typeof(_T));
                        return (_T)aSerializer.ReadObject(aStream);
                    }
#endif
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The deserialization from JSON failed.", err);
                    throw;
                }
            }
        }

#if !WINDOWS_PHONE80 && !WINDOWS_PHONE81
        private Func<Type, XmlObjectSerializer> myDataContractFactoryMethod;
#else
        private Func<Type, DataContractJsonSerializer> myDataContractFactoryMethod;
#endif
        private XmlDictionaryReaderQuotas myDictionaryQuotas;
    }
}

#endif