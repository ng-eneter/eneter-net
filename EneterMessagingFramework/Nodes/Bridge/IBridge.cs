/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System.IO;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Bridge
{
    /// <summary>
    /// Declares the bridge for one-way messaging.
    /// </summary>
    /// <remarks>
    /// The bridge allows to connect applications in case the receiving application uses some different
    /// mechanism for receiving messages.<br/>
    /// E.g. The ASP server uses message handlers (*.ashx files). Therefore, if the ASP server wants to receive
    /// messages from the Silverlight application, then the ASP server can use the bridge component to route messages
    /// received via the message handler.
    /// <br/>
    /// The one-way bridge forwards incoming messages to the real receiver.<br/>
    /// </remarks>
    public interface IBridge : IAttachableOutputChannel
    {
        /// <summary>
        /// Forwards the message to the real receiver.
        /// </summary>
        /// <param name="message">message stored in the stream</param>
        void SendMessage(Stream message);
    }
}

#endif