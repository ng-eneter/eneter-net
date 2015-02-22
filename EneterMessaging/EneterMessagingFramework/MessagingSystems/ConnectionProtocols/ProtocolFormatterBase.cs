/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Abstract internal class providing default implementation for formatting communication before channels.
    /// </summary>
    /// <remarks>
    /// The default implemention of all methods throws NotSupportedException.
    /// </remarks>
    public abstract class ProtocolFormatterBase : IProtocolFormatter
    {
        /// <summary>
        /// Returns encoded 'open connection' message.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <returns</returns>
        public abstract object EncodeOpenConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes 'open connection' message directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public virtual void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        /// <summary>
        /// Returns encoded 'close connection' message.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <returns></returns>
        public abstract object EncodeCloseConnectionMessage(string responseReceiverId);

        /// <summary>
        /// Encodes 'close connection' message directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public virtual void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        /// <summary>
        /// Returns encoded 'request message' or 'response message'.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract object EncodeMessage(string responseReceiverId, object message);

        /// <summary>
        /// Encodes 'request message' or 'response message' directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        /// <param name="outputSream"></param>
        public virtual void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        /// <summary>
        /// Decodes a message from the stream.
        /// </summary>
        /// <param name="readStream"></param>
        /// <returns></returns>
        public virtual ProtocolMessage DecodeMessage(Stream readStream)
        {
            throw new NotSupportedException("This protocol formatter does not support decoding from stream.");
        }

        /// <summary>
        /// Decodes the message.
        /// </summary>
        /// <param name="readMessage"></param>
        /// <returns></returns>
        public abstract ProtocolMessage DecodeMessage(object readMessage);
    }
}
