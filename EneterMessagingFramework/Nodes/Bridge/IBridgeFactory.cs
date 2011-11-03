/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Bridge
{
    /// <summary>
    /// Declares the factory to create Bridge for one-way messaging and DuplexBridge for request-response
    /// messaging. 
    /// </summary>
    public interface IBridgeFactory
    {
        /// <summary>
        /// Creates the bridge to send one-way messages.
        /// </summary>
        /// <returns></returns>
        IBridge CreateBridge();

        /// <summary>
        /// Creates the duplex bridge to send request-response messages.
        /// </summary>
        /// <param name="messagingSystemFactory">messaging system used to create duplex output channel to forward the incoming message</param>
        /// <param name="channelId">channel id where the bridge sends messages.</param>
        /// <returns></returns>
        IDuplexBridge CreateDuplexBridge(IMessagingSystemFactory messagingSystemFactory, string channelId);
    }
}

#endif