/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Implements the serialization/deserialization to/from XmlString.
    /// </summary>
    /// <remarks>
    /// The serializer internally uses XmlSerializer provided by .Net.
    /// <example>
    /// Serialization and deserialization example.
    /// <code>
    /// // Some class to be serialized.
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
    /// XmlStringSerializer aSerializer = new XmlStringSerializer();
    /// object aSerializedObject = aSerializer.Serialize&lt;MyClass&gt;(c);
    /// 
    /// ...
    /// 
    /// // Deserialization
    /// XmlStringSerializer aSerializer = new XmlStringSerializer();
    /// object aDeserializedObject = aSerializer.Deserialize&lt;MyClass&gt;(aSerializedObject);
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public class XmlStringSerializer : ISerializer
    {
#if COMPACT_FRAMEWORK
        /// <summary>
        /// Declaration of the factory method for the Compact Framework platform.
        /// </summary>
        /// <remarks>
        /// Compact Framework does not have delegates Func and Action.
        /// Therefore we need to declare the delegate for the factory method explicitly.
        /// </remarks>
        public delegate XmlSerializer XmlSerializerFactoryMethod(Type typeToSerialize);
#endif

        /// <summary>
        /// Creates the serializer based on XmlSerializer with default settings.
        /// </summary>
        public XmlStringSerializer()
            : this(x => new XmlSerializer(x))
        {
        }

        /// <summary>
        /// Creates the serializer that allows user to specify his own method instantiating XmlSerializer
        /// with desired settings.
        /// </summary>
        /// <param name="xmlSerializerFactoryMethod">
        /// The factory method responsible for creation of XmlSerializer.
        /// The factory method is called during the serialization or deserialization.<br/>
        /// The factory method can be called from more threads at the same time, so be sure, your factory method
        /// is thread safe.
        /// </param>
#if COMPACT_FRAMEWORK
        public XmlStringSerializer(XmlSerializerFactoryMethod xmlSerializerFactoryMethod)
        {
            using (EneterTrace.Entering())
            {
                myXmlSerializerFactoryMethod = (x) => xmlSerializerFactoryMethod(x);
            }
        }
#else
        public XmlStringSerializer(Func<Type, XmlSerializer> xmlSerializerFactoryMethod)
        {
            using (EneterTrace.Entering())
            {
                myXmlSerializerFactoryMethod = xmlSerializerFactoryMethod;
            }
        }
#endif
        
        /// <summary>
        /// Serializes data to the xml string.
        /// </summary>
        /// <remarks>
        /// It internally XmlSerializer provided by .Net.
        /// </remarks>
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
                            XmlSerializer xmlSerializer = myXmlSerializerFactoryMethod(typeof(_T));
                            xmlSerializer.Serialize(anXmlWriter, dataToSerialize);
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
        /// <remarks>
        /// It internally XmlSerializer provided by .Net.
        /// </remarks>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Data to be deserialized.</param>
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
                        XmlSerializer xmlSerializer = myXmlSerializerFactoryMethod(typeof(_T));
                        return (_T)xmlSerializer.Deserialize(aStringReader);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error("The serialization from XML failed.", err);
                    throw;
                }
            }
        }

        private Func<Type, XmlSerializer> myXmlSerializerFactoryMethod;
    }
}
