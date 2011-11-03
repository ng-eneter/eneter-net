/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Nodes.Bridge
{
    /// <summary>
    /// Connects applications using a different mechanism to receive messages.
    /// E.g. A message received from the Silverlight via generic handler (*.ashx file).
    /// </summary>
    /// <remarks>
    /// The bridge allows to connect applications in case the receiving application uses some different
    /// mechanism for receiving messages.<br/>
    /// E.g. The ASP server uses message handlers (*.ashx files). Therefore, if the ASP server wants to receive
    /// messages from the Silverlight application, then the ASP.NET server can use the bridge component to route messages
    /// received via the message handler.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}

#endif