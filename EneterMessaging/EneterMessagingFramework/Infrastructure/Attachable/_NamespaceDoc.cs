/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// Interfaces used by components to be able to attach channels.
    /// </summary>
    /// <remarks>
    /// In order to be able to send messages, all communication components must be able to attach channels.
    /// E.g. if a component needs to send messages and receive responses then it must implement
    /// <see cref="IAttachableDuplexOutputChannel"/> to be able to attach <see cref="IDuplexOutputChannel"/>.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
