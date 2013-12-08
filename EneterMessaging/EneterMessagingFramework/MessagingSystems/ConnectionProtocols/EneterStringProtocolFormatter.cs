/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

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
    public class EneterStringProtocolFormatter : ProtocolFormatterBase<string>, IProtocolFormatter<string>
    {
        /// <summary>
        /// Encodes open connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public override string EncodeOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, responseReceiverId, null);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Encodes close connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public override string EncodeCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, responseReceiverId, null);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Encodes a message or a response message.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <param name="message">the serialized content of the message</param>
        /// <returns></returns>
        public override string EncodeMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, responseReceiverId, message);
                return mySerializer.Serialize<ProtocolMessage>(aMessage) as string;
            }
        }

        /// <summary>
        /// Decodes a message or a response message from the stream.
        /// </summary>
        /// <param name="readStream">stream containing the low-level message</param>
        /// <returns></returns>
        public override ProtocolMessage DecodeMessage(Stream readStream)
        {
            using (EneterTrace.Entering())
            {
                throw new NotSupportedException("Decoding a string message from the stream is not suported.");
            }
        }

        /// <summary>
        /// Decodes a message or a response message from the byte[].
        /// </summary>
        /// <param name="encodedMessage">source of the low-level message</param>
        /// <returns></returns>
        public override ProtocolMessage DecodeMessage(string encodedMessage)
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
                    return myNonProtocolMessage;
                }
            }
        }


        ProtocolMessage IProtocolFormatter.DecodeMessage(object readMessage)
        {
            return DecodeMessage((string)readMessage);
        }


        private static readonly ProtocolMessage myNonProtocolMessage = new ProtocolMessage(EProtocolMessageType.Unknown, "", null);

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
