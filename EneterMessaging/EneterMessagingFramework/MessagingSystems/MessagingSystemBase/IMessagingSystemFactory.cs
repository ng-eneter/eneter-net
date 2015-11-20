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
    /// Represents messaging providing output and input channels for the communication.
    /// </summary>
    /// <remarks>
    /// This factory interface is supposed to be implemented by all messaging systems.
    /// Particular messaging systems are then supposed to provide correct implementations for output and input channels
    /// using their transportation mechanisms. E.g. for TCP, Websockets, ... . 
    /// </remarks>
    public interface IMessagingSystemFactory
    {
        /// <summary>
        /// Creates the output channel which can sends and receive messages from the input channel.
        /// </summary>
        /// <param name="channelId">address of the input channel</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId);

        /// <summary>
        /// Creates the output channel which can sends and receive messages from the input channel.
        /// </summary>
        /// <param name="channelId">address of the input channel</param>
        /// <param name="responseReceiverId">unique identifier of this duplex output channel. If the value is null then
        /// the identifier is genearated automatically</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId);

        /// <summary>
        /// Creates the input channel which can receive and send messages to the output channel.
        /// </summary>
        /// <param name="channelId">address on which the duplex input channel shall listen</param>
        /// <returns>duplex input channel</returns>
        IDuplexInputChannel CreateDuplexInputChannel(string channelId);
    }
}
