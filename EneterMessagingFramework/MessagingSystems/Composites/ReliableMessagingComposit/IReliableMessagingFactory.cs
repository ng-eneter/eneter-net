/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// Declares the factory for the reliable messaging system.
    /// </summary>
    /// <remarks>
    /// The reliable messaging system extends the underlying messaging system by the functionality providing the information
    /// whether the sent message was delivered or not.
    /// <br/><br/>
    /// Sending of the message from the <see cref="IReliableDuplexOutputChannel"/>:<br/>
    /// <ol>
    /// <li>The reliable duplex output channel sends the message.</li>
    /// <li>The reliable duplex input channel receives the message and automatically sends back the acknowledgement.</li>
    /// <li>The reliable duplex output channel receives the acknowledging message and notifies, the message was delivered.</li>
    /// </ol>
    /// <br/><br/>
    /// Sending of the response message from <see cref="IReliableDuplexInputChannel"/>:<br/>
    /// <ol>
    /// <li>The reliable duplex input channel sends the response message.</li>
    /// <li>The reliable duplex output channel receives the message and automatically sends back the acknowledgement.</li>
    /// <li>The reliable duplex input channel receives the acknowledging message and notifies, the response message was delivered.</li>
    /// </ol>
    /// <br/><br/>
    /// Notice, since <see cref="IOutputChannel"/> and <see cref="IInputChannel"/> communicate oneway,
    /// the reliable communication with acknowledge messages is not applicable for them.
    /// Therefore, the factory just uses the underlying messaging system to create them.
    /// </remarks>
    public interface IReliableMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <remarks>
        /// It provides notification whether the sent message was delivered or not.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex output channel</returns>
        new IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId);

        /// <summary>
        /// Creates the reliable duplex output channel that can send messages and receive response messages from <see cref="IReliableDuplexInputChannel"/>.
        /// </summary>
        /// <remarks>
        /// It provides notification whether the sent message was delivered or not.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <param name="responseReceiverId">id of the response receiver</param>
        /// <returns>reliable duplex output channel</returns>
        new IReliableDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId);

        /// <summary>
        /// Creates the reliable duplex input channel that can receiver messages from <see cref="IReliableDuplexOutputChannel"/> and send back
        /// response messages.
        /// </summary>
        /// <remarks>
        /// It provides notification whether the sent response message was delivered or not.
        /// </remarks>
        /// <param name="channelId">channel id, the syntax of the channel id must comply to underlying messaging</param>
        /// <returns>reliable duplex input channel</returns>
        new IReliableDuplexInputChannel CreateDuplexInputChannel(string channelId);
    }
}
