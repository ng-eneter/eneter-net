/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Threading modes used to receive messages and events from output and input channels.
    /// </summary>
    /// <remarks>
    /// Treading dispatching allows to control how duplex output channel and duplex input channel will raise events.
    /// It allows to specify the thread in which events will be invoked. E.g. you can specify that events like
    /// MessageReceived, ConnectionOpened, ConnectionClosed, ... will be raised in the main UI thread.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}

