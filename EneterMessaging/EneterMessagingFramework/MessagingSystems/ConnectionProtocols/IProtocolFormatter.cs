/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.IO;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Declares functionality to encode and decode messages used for the communication between channels. 
    /// </summary>
    /// <typeparam name="T">type of encoded data. It can be byte[] or String.</typeparam>
    public interface IProtocolFormatter<T> : IProtocolFormatter
    {
        /// <summary>
        /// Encodes the open connection request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the duplex output channel to open the connection with the duplex input channel.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client opening the connection.</param>
        /// <returns>encoded message</returns>
        new T EncodeOpenConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes the close connecion request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the duplex output channel or duplex input channel to close the connection.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client that wants to disconnect or that will be disconnected</param>
        /// <returns>encoded message</returns>
        new T EncodeCloseConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes a message or a response message.
        /// </summary>
        /// <remarks>
        /// The message is used by output channel or duplex output channel to send messages or
        /// by duplex input channel to send response messages.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client that wants to send the message. It is empty string if the response message is sent.</param>
        /// <param name="message">serialized message to be sent.</param>
        /// <returns>encoded message</returns>
        new T EncodeMessage(string responseReceiverId, object message);

        /// <summary>
        /// Decodes message from the stream.
        /// </summary>
        /// <param name="readStream">stream to be read</param>
        /// <returns>decoded message</returns>
        new ProtocolMessage DecodeMessage(Stream readStream);

        /// <summary>
        /// Decodes message from the given object.
        /// </summary>
        /// <param name="readMessage">reference to the object.</param>
        /// <returns>decoded message</returns>
        ProtocolMessage DecodeMessage(T readMessage);
    }

    /// <summary>
    /// Declares functionality to encode and decode messages used for the communication between channels.
    /// </summary>
    /// <remarks>
    /// Encoded messages are presented as type of object. This interface is used if it is not needed to know
    /// if the encoded messages are byte[] or string.
    /// </remarks>
    public interface IProtocolFormatter
    {
        /// <summary>
        /// Encodes the open connection request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the duplex output channel to open the connection with the duplex input channel.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client opening the connection.</param>
        /// <returns>encoded message</returns>
        object EncodeOpenConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes the open connection request message to the stream.
        /// </summary>
        /// <param name="responseReceiverId">id of the client opening the connection.</param>
        /// <param name="outputSream">output where the encoded message is written</param>
        void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream);

        /// <summary>
        /// Encodes the close connecion request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the duplex output channel or duplex input channel to close the connection.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client that wants to disconnect or that will be disconnected</param>
        /// <returns>encoded message</returns>
        object EncodeCloseConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes the close connecion request message to the stream.
        /// </summary>
        /// <param name="responseReceiverId">id of the client that wants to disconnect or that will be disconnected</param>
        /// <param name="outputSream">output where the encoded message is written</param>
        void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream);

        /// <summary>
        /// Encodes a message or a response message.
        /// </summary>
        /// <remarks>
        /// The message is used by output channel or duplex output channel to send messages or
        /// by duplex input channel to send response messages.
        /// </remarks>
        /// <param name="responseReceiverId">client id. It is empty string in case of output channel.</param>
        /// <param name="message">message serialized message to be sent.</param>
        /// <returns>encoded message</returns>
        object EncodeMessage(string responseReceiverId, object message);

        /// <summary>
        /// Encodes a message or a response message to the stream.
        /// </summary>
        /// <param name="responseReceiverId">id of the client that wants to send the message. It is empty string if the response message is sent.</param>
        /// <param name="message">serialized message to be sent.</param>
        /// <param name="outputSream">output where the encoded message is written</param>
        void EncodeMessage(string responseReceiverId, object message, Stream outputSream);

        /// <summary>
        /// Decodes message from the stream.
        /// </summary>
        /// <param name="readStream">stream to be read</param>
        /// <returns>decoded message</returns>
        ProtocolMessage DecodeMessage(Stream readStream);

        /// <summary>
        /// Decodes message from the given object.
        /// </summary>
        /// <param name="readMessage">reference to the object.</param>
        /// <returns>decoded message</returns>
        ProtocolMessage DecodeMessage(object readMessage);
    }
}
