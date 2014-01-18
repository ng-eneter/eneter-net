/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Declares service receiving requests via remote procedure calls.
    /// </summary>
    /// <typeparam name="TServiceInterface">
    /// Service interface.
    /// The provided type must be a non-generic interface which can declare methods and events.
    /// </typeparam>
    public interface IRpcService<TServiceInterface> : IAttachableDuplexInputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// The event is invoked when the connection is opened.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the connection is closed.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
    }
}