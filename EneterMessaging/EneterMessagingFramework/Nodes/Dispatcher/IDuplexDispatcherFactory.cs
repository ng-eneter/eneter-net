/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Declares the factory to create the bidirectional dispatcher.
    /// </summary>
    /// <remarks>
    /// The bidirectional dispatcher sends messages to all duplex output channels and also can route back response messages.
    /// </remarks>
    public interface IDuplexDispatcherFactory
    {
        /// <summary>
        /// Creates the duplex dispatcher.
        /// </summary>
        /// <returns>duplex dispatcher</returns>
        IDuplexDispatcher CreateDuplexDispatcher();
    }
}
