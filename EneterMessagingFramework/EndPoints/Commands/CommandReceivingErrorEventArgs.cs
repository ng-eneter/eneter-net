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
    /// Event data when an error was detected during receiving a request in the command.
    /// </summary>
    public class CommandReceivingErrorEventArgs : CommandRequestEventArgs
    {
        /// <summary>
        /// Constructs the event data.
        /// </summary>
        /// <param name="commandProxyId">identifier of the command proxy that sent the request</param>
        /// <param name="receivingError">error detected during receiving the request (e.g. deserialization error)</param>
        public CommandReceivingErrorEventArgs(string commandProxyId, Exception receivingError)
            : base(commandProxyId, "")
        {
            ReceivingError = receivingError;
        }

        /// <summary>
        /// Gets error detected during receiving the request (e.g. deserialization error)
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
