/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System.IO;

namespace Eneter.Messaging.Nodes.Bridge
{
    /// <summary>
    /// Declares the duplex bridge for request-response messaging.
    /// </summary>
    /// <remarks>
    /// The bridge allows to connect applications in case the receiving application uses some different
    /// mechanism for receiving messages.<br/>
    /// E.g. The ASP server uses message handlers (*.ashx files). Therefore, if the ASP server wants to receive
    /// messages from the Silverlight application, then the ASP server can use the bridge component to route messages
    /// received via the message handler.
    /// <br/>
    /// The duplex bridge can be used for the bidirectional communication. It can route both, requests and response messages.<br/>
    /// <br/>
    /// The bridging works like this:<br/>
    /// The Silverlight application sends the message via HTTP to the handler of ASP server.
    /// The ASP server receives the HTTP request in the handler. The implementation of the handler then uses the bridge and its method
    /// <see cref="ProcessRequestResponse"/> to process the request.
    /// <example>
    /// The following example shows an example how to implement the duplex bridge in the Asp Server.
    /// <code>
    /// // Handles messaging communication with Silverlight clients.
    /// public class MessagingHandler : IHttpHandler
    /// {
    ///     public void ProcessRequest(HttpContext context)
    ///     {
    ///         context.Application.Lock();
    ///
    ///         // Get the bridge from the context.
    ///         IDuplexBridge aBridge = context.Application["Bridge"] as IDuplexBridge;
    ///         
    ///         // Ask bridge to route the request and response messages.
    ///         aBridge.ProcessRequestResponse(context.Request.InputStream, context.Response.OutputStream);
    ///
    ///         context.Application.UnLock();
    ///     }
    ///
    ///     public bool IsReusable { get { return false; } }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public interface IDuplexBridge
    {
        /// <summary>
        /// Processes the incoming request.
        /// </summary>
        /// <remarks>
        /// The method reads the "low-level" message from the requestMessage input parameter.<br/>
        /// If the message is 'poll request', then it reads collected response messages and writes them to the responseMessages input parameter.<br/>
        /// If the message is 'open connection request', then it opens the connection with the real receiver.<br/>
        /// If the message is 'close connection request', then it closes the connection with the real receiver.<br/>
        /// If the message is 'message request', then it forwards the message to the real receiver.<br/>
        /// <br/>
        /// The bridge also listens to response messages. If the response message is received, the message is stored until the poll request
        /// will not take it.
        /// </remarks>
        /// <param name="requestMessage">message comming as a request</param>
        /// <param name="responseMessages">messages returned as responses</param>
        void ProcessRequestResponse(Stream requestMessage, Stream responseMessages);
    }
}

#endif