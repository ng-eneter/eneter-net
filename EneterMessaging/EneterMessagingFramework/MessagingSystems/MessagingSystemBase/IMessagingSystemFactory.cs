/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


using Eneter.Messaging.MessagingSystems.ConnectionProtocols;


namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Messaging factory to create output and input channels.
    /// </summary>
    /// <remarks>
    /// This factory interface is supposed to be implemented by all messaging systems.
    /// Particular messaging systems are then supposed to provide correct implementations for output and input channels
    /// using their transportation mechanisms. E.g. for TCP, Websockets, ... . 
    /// </remarks>
    public interface IMessagingSystemFactory
    {
        /// <summary>
        /// Creates the duplex output channel that sends messages to the duplex input channel and receives response messages.
        /// </summary>
        /// <param name="channelId">id representing receiving input channel address.</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId);

        /// <summary>
        /// Creates the duplex output channel that sends messages to the duplex input channel and receives response messages.
        /// </summary>
        /// <param name="channelId">id representing receiving input channel address.</param>
        /// <param name="responseReceiverId">unique identifier of the response receiver represented by this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId);

        /// <summary>
        /// Creates the duplex input channel that receives messages from the duplex output channel and sends back response messages.
        /// </summary>
        /// <param name="channelId">id representing the input channel address.</param>
        /// <returns>duplex input channel</returns>
        IDuplexInputChannel CreateDuplexInputChannel(string channelId);
    }
}
