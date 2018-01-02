/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;

namespace Eneter.Messaging.Nodes.BackupRouter
{
    /// <summary>
    /// The event data available when the original connection is replaced with new connection.
    /// </summary>
    public sealed class RedirectEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="forResponseReceiverId">Client connected to the backup router for which the connection was redirected.</param>
        /// <param name="fromAddress">Original service address.</param>
        /// <param name="toAddress">New service address.</param>
        public RedirectEventArgs(string forResponseReceiverId, string fromAddress, string toAddress)
        {
            ForResponseReceiverId = forResponseReceiverId;
            FromAddress = fromAddress;
            ToAddress = toAddress;
        }
        
        /// <summary>
        /// Gets client connected to the backup router for which the connection was redirected.
        /// </summary>
        public string ForResponseReceiverId { get; private set; }

        /// <summary>
        /// Gets original service address.
        /// </summary>
        public string FromAddress { get; private set; }

        /// <summary>
        /// Gets new service address.
        /// </summary>
        public string ToAddress { get; private set; }
    }
}