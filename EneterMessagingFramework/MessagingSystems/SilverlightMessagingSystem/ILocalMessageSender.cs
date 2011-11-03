/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// Interface declares methods so that the silverlight LocalMessageSender can be wrapped.
    /// </summary>
    public interface ILocalMessageSender
    {
        /// <summary>
        /// Sends the message to the silverlight messaging.
        /// </summary>
        void SendAsync(string message);

        /// <summary>
        /// Returns the receiver name. The receiver name represents the channel id.
        /// </summary>
        string ReceiverName { get; }
    }
}

#endif
