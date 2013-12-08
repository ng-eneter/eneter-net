/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the input connector which provides a basic low-level listening.
    /// </summary>
    internal interface IInputConnector
    {
        /// <summary>
        /// Starts listening to messages.
        /// </summary>
        /// <param name="messageHandler">handler processing incoming messages. If it returns true the connection stays
        /// open and listener can loop for a next messages. If it returns false the listener shall not loop for the
        /// next message.</param>
        void StartListening(Func<MessageContext, bool> messageHandler);

        /// <summary>
        /// Stops listening to messages.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Returns true if the listening is running.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// In case the response receiver address comes inside the message
        /// the duplex input channel calls this method to get the response receiver.
        /// </summary>
        /// <param name="responseReceiverAddress"></param>
        /// <returns></returns>
        ISender CreateResponseSender(string responseReceiverAddress);
    }
}
