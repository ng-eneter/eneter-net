/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.BackupRouter
{
    /// <summary>
    /// Declares the router providing a possibility to reconnect with a backup service in case of a disconnection.
    /// </summary>
    /// <remarks>
    /// The backup router is a component that forwards incoming messages to a connected service (receiver).
    /// In case the connection with the receiver is broken it takes the next service from the list and opens the new connection.
    /// If it is at the end of the list it starts from the beginning.
    /// </remarks>
    public interface IBackupConnectionRouter : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the connection was broken but it was successfully reopened with the next service from the list.
        /// </summary>
        event EventHandler<RedirectEventArgs> ConnectionRedirected;

        /// <summary>
        /// The event is invoked when it failed to connect to all available receivers.
        /// </summary>
        event EventHandler AllRedirectionsFailed;

        /// <summary>
        /// Returns all available receivers.
        /// </summary>
        IEnumerable<string> AvailableReceivers { get; }

        /// <summary>
        /// Adds the service to the list.
        /// </summary>
        /// <param name="channelId">address of the service</param>
        void AddReceiver(string channelId);

        /// <summary>
        /// Adds services to the list.
        /// </summary>
        /// <param name="channelIds">addresses of the service</param>
        void AddReceivers(IEnumerable<string> channelIds);

        /// <summary>
        /// Removes the service from the list. If there are connections to this receiver then they will be closed and redirected.
        /// </summary>
        /// <param name="channelId">address of the service</param>
        void RemoveReceiver(string channelId);

        /// <summary>
        /// Removes the service from the list. It will close all connections.
        /// </summary>
        void RemoveAllReceivers();
    }
}

#endif