/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if SILVERLIGHT3 || SILVERLIGHT4 || SILVERLIGHT5

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Implements the protocol formatter encoding the low-level messages into the string.
    /// </summary>
    /// <remarks>
    /// Encoding to the string can be used if the messaging system does not allow tu use byte[].
    /// E.g. the messaging between Silverlight applications sends only string messages.
    /// </remarks>
    public class EneterStringProtocolFormatter : IProtocolFormatter
    {
        /// <summary>
        /// Encodes open connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, responseReceiverId, null);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        /// <summary>
        /// Encodes close connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, responseReceiverId, null);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        /// <summary>
        /// Encodes a message or a response message.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <param name="message">the serialized content of the message</param>
        /// <returns></returns>
        public object EncodeMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, responseReceiverId, message);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        /// <param name="outputSream"></param>
        public void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support decoding from stream.");
        }

        /// <summary>
        /// Decodes a message or a response message from the byte[].
        /// </summary>
        /// <param name="encodedMessage">source of the low-level message</param>
        /// <returns></returns>
        public ProtocolMessage DecodeMessage(object encodedMessage)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    ProtocolMessage aProtocolMessage = mySerializer.Deserialize<ProtocolMessage>(encodedMessage);
                    return aProtocolMessage;
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "detected that the encoded message is not serialized XML of ProtocolMessage class.", err);
                    return null;
                }
            }
        }

        /// <summary>
        /// Throws NotSupportedException.
        /// </summary>
        /// <param name="readStream"></param>
        /// <returns></returns>
        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            throw new NotSupportedException("This protocol formatter does not support decoding from stream.");
        }


        private ISerializer mySerializer = new XmlStringSerializer();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}

#endif