/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the low-level sender of request messages and response messages.
    /// </summary>
    internal interface ISender
    {
        void SendMessage(object message);
    }
}
