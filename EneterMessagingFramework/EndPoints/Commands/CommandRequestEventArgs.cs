/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Event data when a request was received in the command.
    /// </summary>
    public class CommandRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event data.
        /// </summary>
        /// <param name="commandProxyId">identifier of the command proxy that sent the request</param>
        /// <param name="commandId">command identifier</param>
        public CommandRequestEventArgs(string commandProxyId, string commandId)
        {
            CommandProxyId = commandProxyId;
            CommandId = commandId;
        }

        /// <summary>
        /// Gets the identifier of the command proxy that sent the request.
        /// </summary>
        public string CommandProxyId { get; private set; }

        /// <summary>
        /// Gets command identifier.
        /// </summary>
        public string CommandId { get; private set; }
    }
}
