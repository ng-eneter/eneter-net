/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Implements the factory to create the one-way dispatcher.
    /// </summary>
    public class DispatcherFactory : IDispatcherFactory
    {
        /// <summary>
        /// Creates the dispatcher.
        /// </summary>
        public IDispatcher CreateDispatcher()
        {
            using (EneterTrace.Entering())
            {
                return new Dispatcher();
            }
        }
    }
}
