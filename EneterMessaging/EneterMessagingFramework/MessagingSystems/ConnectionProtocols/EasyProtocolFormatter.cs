/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Simple and very fast encoding/decoding (only for TCP, WebSocket and multicast/broadcast UDP).
    /// </summary>
    /// <remarks>
    /// This protocol encoding can be used only for TCP, WebSocket or multicast/broadcast UDP communication.
    /// The simplicity of this formatting provides a high performance and easy portability to various platforms allowing so
    /// to communicate with Eneter even without Eneter.<br/>
    /// However this formatting has certain limitations:
    /// <ul>
    /// <li>It can be used only for TCP or WebSocket based communication or UDP multicasting and broadcasting.</li>
    /// <li>It cannot be used if the reconnect is needed. It means it cannot be used in buffered messaging.</li>
    /// </ul>
    /// <b>Encoding of open connection message:</b><br/>
    /// N.A. - the open connection message is not used. The connection is considered open when the socket is open.<br/>
    /// <br/>
    /// <b>Encoding of close connection message:</b><br/>
    /// N.A. = the close connection message is not used. The connection is considered closed when the socket is closed.<br/>
    /// <br/>
    /// <b>Encoding of data message:</b><br/>
    /// 1 byte - type of data: 10 string in UTF8, 40 bytes<br/>
    /// 4 bytes - length: 32 bit integer indicating the size (in bytes) of message data.<br/>
    /// x bytes - message data<br/>
    /// <br/>
    /// The 32 bit integer indicating the length of message data is encoded as little endian byte default.
    /// If big endian is needed it is possible to specify it in the constructor.
    /// <example>
    /// The following example shows how to use TCP messaging with EasyProtocolFormatter:<br/>
    /// <code>
    /// // Instantiate protocol formatter.
    /// IProtocolFormatter aFormatter = new EasyProtocolFormatter();
    /// 
    /// // Provide the protocol formatter into the messaging constructor.
    /// // Note: It can be only TCP or WebSocket messaging.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory(aFormatter);
    /// ...
    /// // Then use can use the messaging to create channels.
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8084/");
    /// ...
    /// </code>
    /// </example>
    /// </remarks>
    public class EasyProtocolFormatter : IProtocolFormatter
    {
        /// <summary>
        /// Constructs the protocol formatter with default little endian encoding.
        /// </summary>
        public EasyProtocolFormatter()
            : this(true)
        {
        }

        /// <summary>
        /// Constructs the protocol formatter with specified endianess.
        /// </summary>
        /// <param name="isLittleEndian">true - little endian, false - big endian.</param>
        public EasyProtocolFormatter(bool isLittleEndian)
        {
            myIsLittleEndian = isLittleEndian;
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <remarks>
        /// The open connection message is not used. Therefore it returns null.
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
        /// The open connection message is not used. Therefore it does not write any data to the provided output stream.
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
        /// The close connection message is not used. Therefore it returns null.
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
        /// The close connection message is not used. Therefore it does not write any data to the provided output stream.
        /// </remarks>
        /// <param name="responseReceiverId">not used</param>
        /// <param name="outputSream">not used</param>
        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            // An explicit close connection message is not supported therefore write nothing into the stream.
        }

        /// <summary>
        /// Encodes given message to bytes.
        /// </summary>
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
                return null;
            }
            catch (Exception err)
            {
                EneterTrace.Warning("Failed to decode the message.", err);
                return null;
            }
        }

        private bool myIsLittleEndian;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
