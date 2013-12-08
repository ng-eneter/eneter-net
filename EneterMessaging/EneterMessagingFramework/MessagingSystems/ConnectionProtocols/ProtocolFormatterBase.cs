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
    /// <typeparam name="T"></typeparam>
    public abstract class ProtocolFormatterBase<T> : IProtocolFormatter<T>
    {
        /// <summary>
        /// Returns encoded 'open connection' message.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <returns></returns>
        public abstract T EncodeOpenConnectionMessage(string responseReceiverId);

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
        public abstract T EncodeCloseConnectionMessage(string responseReceiverId);

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
        public abstract T EncodeMessage(string responseReceiverId, object message);

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
        public abstract ProtocolMessage DecodeMessage(T readMessage);

        object IProtocolFormatter.EncodeOpenConnectionMessage(string responseReceiverId)
        {
            return EncodeOpenConnectionMessage(responseReceiverId);
        }

        object IProtocolFormatter.EncodeCloseConnectionMessage(string responseReceiverId)
        {
            return EncodeCloseConnectionMessage(responseReceiverId);
        }

        object IProtocolFormatter.EncodeMessage(string responseReceiverId, object message)
        {
            return EncodeMessage(responseReceiverId, message);
        }

        ProtocolMessage IProtocolFormatter.DecodeMessage(object readMessage)
        {
            return DecodeMessage((T)readMessage);
        }
    }
}
