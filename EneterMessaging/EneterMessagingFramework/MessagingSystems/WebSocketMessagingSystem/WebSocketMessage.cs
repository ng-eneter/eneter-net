/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

using System;
using System.IO;
using System.Text;

using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// Represents a data message received via websocket communication.
    /// </summary>
    public sealed class WebSocketMessage : EventArgs
    {
        /// <summary>
        /// Constructs the message - only eneter framework can construct it.
        /// </summary>
        /// <param name="isText">true if it is a text message.</param>
        /// <param name="inputStream"></param>
        internal WebSocketMessage(bool isText, Stream inputStream)
        {
            IsText = isText;
            InputStream = inputStream;
        }

        /// <summary>
        /// Returns the whole incoming message.
        /// </summary>
        /// <remarks>
        /// In case the message was sent via multiple frames it waits until all frames are
        /// collected and then returns the result message.
        /// </remarks>
        /// <returns>received message</returns>
        public byte[] GetWholeMessage()
        {
            using (EneterTrace.Entering())
            {
                return StreamUtil.ReadToEnd(InputStream);
            }

        }

        /// <summary>
        /// Returns the whole incoming text message.
        /// </summary>
        /// <remarks>
        /// In case the message was sent via multiple frames it waits until all frames are
        /// collected and then returns the result message.<br/>
        /// To receive message as a text message, according to websocket protocol the message
        /// must be sent via the text frame.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The message wsa not sent/received as the text message.
        /// I.e. the message was not sent via the text frame.
        /// </exception>
        /// <returns>received text message</returns>
        public string GetWholeTextMessage()
        {
            if (!IsText)
            {
                throw new InvalidOperationException("This is not text message. WebSocketMessage.IsText != true.");
            }

            byte[] aMessageContent = GetWholeMessage();

            return Encoding.UTF8.GetString(aMessageContent, 0, aMessageContent.Length);
        }

        /// <summary>
        /// Returns true if the message is text. The message is text when sent via text frame.
        /// </summary>
        public bool IsText { get; private set; }

        /// <summary>
        /// Returns the input stream user can use to read the message from.
        /// </summary>
        /// <remarks>
        /// The reading of the stream blocks if desired amount of data is not available and
        /// not all message frames were received.
        /// </remarks>
        public Stream InputStream { get; private set; }
    }
}


#endif