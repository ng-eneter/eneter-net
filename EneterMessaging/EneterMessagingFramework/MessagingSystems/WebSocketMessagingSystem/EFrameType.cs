/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// Defines data frames as specified by the websocket protocol.
    /// </summary>
    internal enum EFrameType
    {
        /// <summary>
        /// Frame contains message data that was not sent in one 'Text' or 'Binary'.
        /// Message that is split into multiple frames.
        /// </summary>
        Continuation = 0x0,

        /// <summary>
        /// Frame contains UTF8 text message data.
        /// </summary>
        Text = 0x1,

        /// <summary>
        /// Frame contains binary data.
        /// </summary>
        Binary = 0x2,

        /// <summary>
        /// Control frame indicating the connection goes down.
        /// </summary>
        Close = 0x8,

        /// <summary>
        /// Control frame pinging the end-point (client or server). The pong response is expected.
        /// </summary>
        Ping = 0x9,

        /// <summary>
        /// Control frame as a response for the ping.
        /// </summary>
        Pong = 0xA
    }
}

#endif