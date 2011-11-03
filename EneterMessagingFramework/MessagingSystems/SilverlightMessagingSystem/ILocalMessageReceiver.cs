/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// Interface declares methods so that the silverlight LocalMessageReceiver can be wrapped.
    /// </summary>
    public interface ILocalMessageReceiver : IDisposable
    {
        /// <summary>
        /// Event is invoked when the message is received from the input channel.
        /// </summary>
        event EventHandler<ChannelMessageEventArgs> MessageReceived;

        /// <summary>
        /// Returns the receiver name. The receiver name represents the channel id.
        /// </summary>
        string ReceiverName { get; }

        /// <summary>
        /// Starts listening from the silverlight messaging system.
        /// </summary>
        void Listen();
    }
}

#endif
