/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.EndPoints.TypedMessages
{

    /// <summary>
    /// Represents an empty data type 'void'.
    /// Can be used if no type is expected as a message.
    /// </summary>
    /// <remarks>
    /// <example>
    /// The following example shows how to use VoidMessage to declare a message sender
    /// sending string messages and receiving "nothing".
    /// <code>
    /// ...
    /// IDuplexTypedMessageSender&lt;VoidMessage, string&gt; myMessageSender;
    /// ...
    /// </code>
    /// </example>
    /// </remarks>
    [Serializable]
    [DataContract]
    public class VoidMessage
    {
    }
}
