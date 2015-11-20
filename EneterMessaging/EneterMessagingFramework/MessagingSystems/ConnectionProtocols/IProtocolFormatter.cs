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
    /// <remarks>
    /// The implementaton of this interface defines how 'open connection', 'close connection' and 'message data'
    /// messages shall be encoded and decoded during communication between channels.
    /// </remarks>
    public interface IProtocolFormatter
    {
        /// <summary>
        /// Encodes the open connection request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the output channel to open the connection with the input channel.<br/>
        /// If the open connection message is not used it shall return null. 
        /// </remarks>
        /// <param name="responseReceiverId">id of the client opening the connection.</param>
        /// <returns>encoded open connection message.</returns>
        object EncodeOpenConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes the open connection request message to the stream.
        /// </summary>
        /// <remarks>
        /// The message is used by the output channel to open the connection with the input channel.<br/>
        /// If the open connection message is not used it shall just return without writing to the stream.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client opening the connection.</param>
        /// <param name="outputSream">output where the encoded message is written</param>
        void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream);

        /// <summary>
        /// Encodes the close connecion request message.
        /// </summary>
        /// <remarks>
        /// The message is used by the output channel to close the connection with the input channel.
        /// It is also used by input channel when it disconnects the output channel.<br/>
        /// If the close connection message is not used it shall return null.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client that wants to disconnect or that will be disconnected</param>
        /// <return>sencoded close connection message</returns>
        object EncodeCloseConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes the close connecion request message to the stream.
        /// </summary>
        /// <remarks>
        /// The message is used by the output channel to close the connection with the input channel.
        /// It is also used by input channel when it disconnects the output channel.<br/>
        /// If the close connection message is not used it shall just return without writing to the stream.
        /// </remarks>
        /// <param name="responseReceiverId">id of the client that wants to disconnect or that will be disconnected</param>
        /// <param name="outputSream">output where the encoded message is written</param>
        void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream);

        /// <summary>
        /// Encodes the data message.
        /// </summary>
        /// <remarks>
        /// The message is used by the output as well as input channel to send the data message.
        /// </remarks>
        /// <param name="responseReceiverId">client id. It is empty string in case of output channel.</param>
        /// <param name="message">message serialized message to be sent.</param>
        /// <return>sencoded data message</returns>
        object EncodeMessage(string responseReceiverId, object message);

        /// <summary>
        /// Encodes the data message into the stream.
        /// </summary>
        /// <remarks>
        /// The message is used by the output as well as input channel to send the data message.
        /// </remarks>
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
