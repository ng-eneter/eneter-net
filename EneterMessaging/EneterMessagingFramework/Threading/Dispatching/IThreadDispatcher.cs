/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Threading mode used by messaging systems to raise events and deliver messages.
    /// </summary>
    public interface IThreadDispatcher
    {
        /// <summary>
        /// Invokes the given delegate in the desired thread.
        /// </summary>
        /// <param name="workItem">delegate to be invoked</param>
        void Invoke(Action workItem);
    }
}
