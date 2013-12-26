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
    public interface IRpcService<TServiceInterface> : IAttachableDuplexInputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// The event is invoked when a duplex typed message sender opened the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a duplex typed message sender closed the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
    }
}