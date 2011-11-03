﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    /// <summary>
    /// Declares the load balancer.
    /// </summary>
    /// <remarks>
    /// The load balancer maintains a list of receivers processing a certain request.
    /// When the balancer receives the request, it chooses which receiver shall process it,
    /// so that all receivers are loaded optimally.
    /// </remarks>
    public interface ILoadBalancer : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the client sending requests was connected to the balancer.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when the client sending requests was disconnected from the balanacer.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Adds the request receiver to the load balancer.
        /// </summary>
        /// <param name="channelId">channel id (address) of the receiver processing requests.</param>
        void AddDuplexOutputChannel(string channelId);

        /// <summary>
        /// Removes the request receiver from the load balancer.
        /// </summary>
        /// <param name="channelId">channel id (address) of the receiver processing requests.</param>
        void RemoveDuplexOutputChannel(string channelId);

        /// <summary>
        /// Removes all request receiers from the load balanacer.
        /// </summary>
        void RemoveAllDuplexOutputChannels();
    }
}

#endif