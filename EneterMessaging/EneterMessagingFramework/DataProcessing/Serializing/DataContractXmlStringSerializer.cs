/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Implements the serialization/deserialization to/from an xml string.
    /// The serializer is based on DataContractSerializer provided by .Net and it supports
    /// attributes (DataContract, DataMember, ...) specifying how data is serialized/deserialized.
    /// </summary>
    public class DataContractXmlStringSerializer : ISerializer
    {
        /// <summary>
        /// Creates the serializer based on DataContractSerializer with default settings.
        /// </summary>
        public DataContractXmlStringSerializer()
            : this(x => new DataContractSerializer(x))
        {
        }

        /// <summary>
        /// Creates the serializer that allows user to specify his own method to instantiate DataContractSerializer
        /// with desired settings.
        /// </summary>
        /// <param name="dataContractFactoryMethod">
        /// The factory method responsible for creation of DataContractSerializer.
        /// The factory method is called during the serialization or deserialization.<br/>
        /// The factory method can be called from more threads at the same time, so be sure, your factory method
        /// is thread safe.
        /// </param>
        public DataContractXmlStringSerializer(Func<Type, XmlObjectSerializer> dataContractFactoryMethod)
        {
            using (EneterTrace.Entering())
            {
                myDataContractFactoryMethod = dataContractFactoryMethod;
            }
        }

        /// <summary>
        /// Serializes data to the xml string.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>xml string</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                string aSerializedObjectStr = "";

                try
                {
                    using (StringWriter aStringWriter = new StringWriter())
                    {
                        XmlWriterSettings anXmlSettings = new XmlWriterSettings();
                        anXmlSettings.OmitXmlDeclaration = true;
                        using (XmlWriter anXmlWriter = XmlWriter.Create(aStringWriter, anXmlSettings))
                        {
                            XmlObjectSerializer xmlSerializer = myDataContractFactoryMethod(typeof(_T));
                            xmlSerializer.WriteObject(anXmlWriter, dataToSerialize);
                        }

                        aSerializedObjectStr = aStringWriter.ToString();
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The serialization to XML failed.", err);
                    throw;
                }

                return aSerializedObjectStr;
            }
        }

        /// <summary>
        /// Deserializes data into the specified type.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Data serialized in xml string.</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                string aSerializedXml = (string)serializedData;

                try
                {
                    using (StringReader aStringReader = new StringReader(aSerializedXml))
                    {
                        using (XmlReader anXmlReader = XmlReader.Create(aStringReader))
                        {
                            XmlObjectSerializer xmlSerializer = myDataContractFactoryMethod(typeof(_T));
                            return (_T)xmlSerializer.ReadObject(anXmlReader);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The deserialization from XML failed.", err);
                    throw;
                }
            }
        }


        private Func<Type, XmlObjectSerializer> myDataContractFactoryMethod;
    }
}

#endif