/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that invokes methods according to its threading model.
    /// </summary>
    public interface IThreadDispatcherProvider
    {
        /// <summary>
        /// Returns dispatcher that can invoke methods according to its threading model.
        /// </summary>
        /// <returns></returns>
        IThreadDispatcher GetDispatcher();
    }
}
