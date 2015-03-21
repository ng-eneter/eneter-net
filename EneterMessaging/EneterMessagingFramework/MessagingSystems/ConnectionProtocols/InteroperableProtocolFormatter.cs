/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Trivial and very fast communication protocol which is easy to use from various platforms.
    /// </summary>
    /// <remarks>
    /// This is a very simple and fast communication protocol which is easy to implement and so it can be used for the communication
    /// with Eneter event in cases Eneter is not available in all devices. <br/>
    /// E.g. if the deployment consists of a .NET based eneter service and .NET and Java based eneter clients and in addition you would like to 
    /// add other clients which cannot use Eneter framework (e.g. embedded devices or IoT (Interner of Things) devices which have limitted
    /// resources. <br/>
    /// <br/>
    /// The protocol encodes messages following way:
    /// <ul>
    /// <li>Open Connection Message - it is not used by this protocol. Openning the connection via the socket is sufficiant.</li>
    /// <li>Close Connection Message - it is not used by this protocol. Closing the socket is sufficiant.</li>
    /// <li>Message:
    ///     <ul>
    ///     <li>1 byte : if 10 then following data is UTF8 string. If 40 then following data is byte array.</li>
    ///     <li>4 bytes: size of following data. The size can be encoded in little endian or big endian (see constructor).</li>
    ///     <li>n bytes: message data of specified size.</li>
    ///     </ul>
    /// </li>
    /// </ul>
    /// Note:<br/>
    /// This protocol can be used only with TCP or WebSocket.<br/>
    /// It is not possible to use this protocol in buffered messaging with automatic reconnect.
    /// </remarks>
    public class InteroperableProtocolFormatter : IProtocolFormatter
    {
        /// <summary>
        /// Constructs the protocol which will use little endian encoding.
        /// </summary>
        public InteroperableProtocolFormatter()
            : this(true)
        {
        }

        /// <summary>
        /// Constructs the protocol.
        /// </summary>
        /// <param name="isLittleEndian">true - little endian encoding. false - big endian encoding</param>
        public InteroperableProtocolFormatter(bool isLittleEndian)
        {
            myIsLittleEndian = isLittleEndian;
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <remarks>
        /// This protocol does not use explicit open connection message to open the connection.
        /// Instead of that openning the connection via socket is sufficient.
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <returns>null</returns>
        public object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            // An explicit open connection message is not supported.
            return null;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <remarks>
        /// This protocol does not use explicit open connection message to open the connection.
        /// Instead of that openning the connection via socket is sufficient.
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <param name="outputSream">not used</param>
        public void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            // An explicit open connection message is not supported therefore write nothing into the sream.
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <remarks>
        /// This protocol does not use explicit close connection message to close the connection.
        /// Instead of that closing the socket is sufficient.
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <returns>null</returns>
        public object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            // An explicit close connection message is not supported.
            return null;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <remarks>
        /// This protocol does not use explicit close connection message to close the connection.
        /// Instead of that closing the socket is sufficient.
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <param name="outputSream">not used</param>
        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            // // An explicit close connection message is not supported therefore write nothing into the stream.
        }

        /// <summary>
        /// Encodes given message to bytes.
        /// </summary>
        /// <remarks>
        /// It encodes the message into the following byte sequence:
        /// <ul>
        /// <li>1 byte : if 10 then following data is UTF8 string. If 40 then following data is byte array.</li>
        /// <li>4 bytes: size of following data. The size can be encoded in little endian or big endian (see constructor).</li>
        /// <li>n bytes: message data of specified size.</li>
        /// </ul>
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <param name="message">message serialized in string or byte[]</param>
        /// <returns>message encoded in byte[]</returns>
        public object EncodeMessage(string responseReceiverId, object message)
        {
            //using (EneterTrace.Entering())
            //{
                using (MemoryStream aBuffer = new MemoryStream())
                {
                    EncodeMessage(responseReceiverId, message, aBuffer);
                    return aBuffer.ToArray();
                }
            //}
        }

        /// <summary>
        /// Encodes given message to bytes.
        /// </summary>
        /// <remarks>
        /// It encodes the message into the following byte sequence:
        /// <ul>
        /// <li>1 byte : if 10 then following data is UTF8 string. If 40 then following data is byte array.</li>
        /// <li>4 bytes: size of following data. The size can be encoded in little endian or big endian (see constructor).</li>
        /// <li>n bytes: message data of specified size.</li>
        /// </ul>
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <param name="message">message serialized in string or byte[]</param>
        /// <param name="outputSream">stream into which the message will be encoded</param>
        public void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            //using (EneterTrace.Entering())
            //{
                BinaryWriter aWriter = new BinaryWriter(outputSream);
                myEncoderDecoder.Write(aWriter, message, myIsLittleEndian);
            //}
        }

        /// <summary>
        /// Decodes the sequence of bytes.
        /// </summary>
        /// <param name="readMessage">encoded bytes.</param>
        /// <returns>protocol message containing the decoded message</returns>
        public ProtocolMessage DecodeMessage(object readMessage)
        {
            using (MemoryStream aMemoryStream = new MemoryStream((byte[])readMessage))
            {
                return DecodeMessage((Stream)aMemoryStream);
            }
        }

        /// <summary>
        /// Decodes the sequence of bytes.
        /// </summary>
        /// <param name="readStream">stream containing bytes to be decoded</param>
        /// <returns>protocol message containing the decoded message</returns>
        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            try
            {
                BinaryReader aReader = new BinaryReader(readStream);
                object aMessageData = myEncoderDecoder.Read(aReader, myIsLittleEndian);

                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, "", aMessageData);
                return aProtocolMessage;
            }
            catch (EndOfStreamException)
            {
                // End of the stream.
                return null;
            }
            catch (IOException)
            {
                // End of the stream. (e.g. underlying socket was closed)
                return null;
            }
            catch (ObjectDisposedException)
            {
                // End of the stream.
                return null;
            }
        }

        private bool myIsLittleEndian;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
