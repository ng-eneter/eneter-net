/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// The reliable messaging, providing the confirmation if the sent message was delivered.
    /// </summary>
    /// <remarks>
    /// The reliable messaging provides the information whether the sent message was delivered.
    /// If the sent message was delivered, the receiver automatically sends back the acknowledge message.
    /// If the sender receives the acknowledge message, it notifies, the message was delivered.
    /// If the sender does not receive the acknowledge message within a specified timeout, it notifies, the message
    /// was not delivered.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
