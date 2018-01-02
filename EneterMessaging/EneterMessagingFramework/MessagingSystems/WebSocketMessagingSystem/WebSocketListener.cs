/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// WebSocket server.
    /// </summary>
    /// <remarks>
    /// <example>
    /// The following example implements a simple service echoing the incoming message back to the client.
    /// <code>
    /// using System;
    /// using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
    /// 
    /// namespace EchoService
    /// {
    ///    class Program
    ///    {
    ///        static void Main(string[] args)
    ///        {
    ///            WebSocketListener aService = new WebSocketListener(new Uri("ws://127.0.0.1:8045/Echo/"));
    /// 
    ///            aService.StartListening(client =>
    ///            {
    ///                WebSocketMessage aMessage;
    ///                while ((aMessage = client.ReceiveMessage()) != null)
    ///                {
    ///                    object aData;
    ///                    if (aMessage.IsText)
    ///                    {
    ///                        aData = aMessage.GetWholeTextMessage();
    ///                    }
    ///                    else
    ///                    {
    ///                        aData = aMessage.GetWholeMessage();
    ///                    }
    /// 
    ///                    // Send echo.
    ///                    client.SendMessage(aData);
    ///                }
    ///            });
    /// 
    ///            Console.WriteLine("WebSocket service is listening. Press Enter to stop.");
    ///            Console.ReadLine();
    /// 
    ///            aService.StopListening();
    ///        }
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class WebSocketListener
    {
        /// <summary>
        /// Wraps the the implementation of the path listener to a separate class because PathListenerProviderBase
        /// shall be visible only internally.
        /// In addition, the documentation needs to be generated for WebSocketListener - therefore all methods
        /// would have to be overriden to have its own specific help description.
        /// </summary>
        private class WebSocketListenerImpl : PathListenerProviderBase<IWebSocketClientContext>
        {
            public WebSocketListenerImpl(string absoluteUri, bool reuseAddressFlag, int maxAmountOfConnections)
                : base(new WebSocketHostListenerFactory(reuseAddressFlag, maxAmountOfConnections), new Uri(absoluteUri, UriKind.Absolute))
            {
            }

            public WebSocketListenerImpl(Uri uri, bool reuseAddressFlag, int maxAmountOfConnections)
                : base(new WebSocketHostListenerFactory(reuseAddressFlag, maxAmountOfConnections), uri)
            {
            }

            public WebSocketListenerImpl(Uri uri, ISecurityFactory securityFactory, bool reuseAddressFlag, int maxAmountOfConnections)
                : base(new WebSocketHostListenerFactory(reuseAddressFlag, maxAmountOfConnections), uri, securityFactory)
            {
            }

            protected override string TracedObject { get { return "WebSocketListener "; } }
        }


        /// <summary>
        /// Construct websocket service.
        /// </summary>
        /// <param name="webSocketUri">service address. Provide port number too.</param>
        public WebSocketListener(Uri webSocketUri)
        {
            MaxAmountOfClients = -1;
            myListenerFactoryMethod = () => new WebSocketListenerImpl(webSocketUri, ReuseAddress, MaxAmountOfClients);
        }

        /// <summary>
        /// Construct websocket service.
        /// </summary>
        /// <param name="webSocketUri">service address. Provide port number too.</param>
        /// <param name="securityFactory">
        /// Factory allowing SSL communication. <see cref="ServerSslFactory"/>
        /// </param>
        public WebSocketListener(Uri webSocketUri, ISecurityFactory securityFactory)
        {
            MaxAmountOfClients = -1;
            myListenerFactoryMethod = () => new WebSocketListenerImpl(webSocketUri, securityFactory, ReuseAddress, MaxAmountOfClients);
        }

        /// <summary>
        /// Starts listening.
        /// </summary>
        /// <remarks>
        /// To handle connected clients the connectionHandler delegate is called. The connectionHandler delegate
        /// is called in parallel from multiple threads as clients are connected.
        /// </remarks>
        /// <param name="connectionHandler">callback delegate handling incoming connections. It is called 
        /// from multiple threads.</param>
        public void StartListening(Action<IWebSocketClientContext> connectionHandler)
        {
            if (myListenerImpl == null)
            {
                myListenerImpl = myListenerFactoryMethod();
            }
            myListenerImpl.StartListening(connectionHandler);
        }

        /// <summary>
        /// Stops listening and closes all open connections with clients.
        /// </summary>
        public void StopListening()
        {
            if (myListenerImpl != null)
            {
                myListenerImpl.StopListening();
            }
        }

        /// <summary>
        /// Returns true if the service is listening.
        /// </summary>
        public bool IsListening
        {
            get
            {
                return myListenerImpl != null && myListenerImpl.IsListening;
            }
        }

        /// <summary>
        /// Sets or gets the flag indicating whether the socket can be bound to the address which is already used.
        /// </summary>
        /// <remarks>
        /// If the value is true then the duplex input channel can start listening to the IP address and port which is already used by other channel.
        /// </remarks>
        public bool ReuseAddress { get; set; }

        /// <summary>
        /// Sets or gets the maximum amount of clients which can connect the the listener.
        /// </summary>
        /// <remarks>
        /// The default value is -1 which means the amount of connected clients is not restrected.<br/>
        /// If the connecting client exceeds the maximum amount the client is not connected and its TCP socket is closed.
        /// </remarks>
        public int MaxAmountOfClients { get; set; }

        private Func<WebSocketListenerImpl> myListenerFactoryMethod;
        private WebSocketListenerImpl myListenerImpl;
    }
}


#endif