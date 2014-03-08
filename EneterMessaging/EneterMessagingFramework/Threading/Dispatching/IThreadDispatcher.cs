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
    /// Invokes a method according to specified thread mode.
    /// </summary>
    public interface IThreadDispatcher
    {
        /// <summary>
        /// Invokes method in desired thread.
        /// </summary>
        /// <param name="workItem">delegate to be invoked</param>
        void Invoke(Action workItem);
    }
}
