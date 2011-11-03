/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Declares the factory to create the broker and the broker client.
    /// </summary>
    public interface IDuplexBrokerFactory
    {
        /// <summary>
        /// Creates the broker client.
        /// </summary>
        /// <remarks>
        /// The broker client is able to send messages to the broker (via attached duplex output channel).
        /// It also can subscribe for messages to receive notifications from the broker.
        /// </remarks>
        IDuplexBrokerClient CreateBrokerClient();

        /// <summary>
        /// Creates the broker.
        /// </summary>
        /// <remarks>
        /// The broker receives messages and forwards them to subscribers.
        /// </remarks>
        IDuplexBroker CreateBroker();
    }
}
