/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Declares the factory creating the one-way dispatcher.
    /// </summary>
    /// <remarks>
    /// The one-way dispatcher sends messages to all attached output channels.
    /// </remarks>
    public interface IDispatcherFactory
    {
        /// <summary>
        /// Creates the dispatcher.
        /// </summary>
        IDispatcher CreateDispatcher();
    }
}
