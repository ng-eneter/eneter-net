/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that shall be used for invoking events and delivering messages in a correct thread.
    /// </summary>
    /// <remarks>
    /// Having this provider allows to provide the same dispatcher across multiple messaging systems.
    /// E.g. if you receive messages from multiple messaging and you want that all of them are delivered in particular thread.
    /// </remarks>
    public interface IThreadDispatcherProvider
    {
        /// <summary>
        /// Returns dispatcher that can invoke methods according to its threading model.
        /// </summary>
        /// <returns></returns>
        IThreadDispatcher GetDispatcher();
    }
}
