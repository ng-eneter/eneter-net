/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Internal interface for implementing message systems.
    /// Note: This is just a helper interface helping implementation of some messaging system.
    ///       It is not mandatory to use this interface to implement the messaging system.
    /// </summary>
    internal interface IMessagingSystemBase
    {
        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="channelId">channel id receiving the message</param>
        /// <param name="message">message to be sent</param>
        void SendMessage(string channelId, object message);

        /// <summary>
        /// Registers the listener.
        /// </summary>
        /// <param name="channelId">channel id registering for receiving messages</param>
        /// <param name="messageHandler">method handling the incoming message</param>
        void RegisterMessageHandler(string channelId, Action<object> messageHandler);

        /// <summary>
        /// Unregisters the listener.
        /// </summary>
        /// <param name="channelId">channel id to be unregistered from listening</param>
        void UnregisterMessageHandler(string channelId);
    }
}
