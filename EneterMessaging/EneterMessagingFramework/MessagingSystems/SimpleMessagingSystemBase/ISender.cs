/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using System.IO;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the low-level sender of request messages and response messages.
    /// </summary>
    /// <remarks>
    /// IOutputConnector is derived from ISender so that it can send messages.
    /// IInputConnector uses ISender to send back response messages.
    /// </remarks>
    internal interface ISender
    {
        /// <summary>
        /// If it returns true then SendMessage(Action&lt;Stream&gt; toStreamWritter) will be used to send messages.
        /// </summary>
        /// <remarks>
        /// The point is that sometimes are messages sent using a stream and sometimes not. If this property
        /// returns true then the implementation of the sender expects messages are sent using the stream.
        /// </remarks>
        bool IsStreamWritter { get; }

        /// <summary>
        /// Sends a message NOT using the stream. This method does not have to be implemented if IsStreamWritter returns false.
        /// </summary>
        /// <param name="message"></param>
        void SendMessage(object message);

        /// <summary>
        /// Sends a message using the stream. This method does not have to be implemented if IsStreamWritter returns true.
        /// </summary>
        /// <param name="toStreamWritter"></param>
        void SendMessage(Action<Stream> toStreamWritter);
    }
}
