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
    /// Declares the factory that creates communication channels.
    /// </summary>
    public interface IMessagingSystemFactory
    {
        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId);

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method allows to specified a desired response receiver id. Please notice, the response receiver
        /// id is supposed to be unique.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">identifies the receiving duplex input channel</param>
        /// <param name="responseReceiverId">unique identifier of the response receiver represented by this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId);

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">identifies the address, the duplex input channel listens to</param>
        /// <returns>duplex input channel</returns>
        IDuplexInputChannel CreateDuplexInputChannel(string channelId);
    }
}
