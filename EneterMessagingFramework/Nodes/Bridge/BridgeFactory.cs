/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Bridge
{
    /// <summary>
    /// Implements the factory to create Bridge and Duplex Bridge.
    /// </summary>
    /// <remarks>
    /// Bridge is intended to connect a different mechanism for receiving messages with the Eneter Framework.
    /// E.g. If ASP.NET server receives messages from its Silverlight client via the generic handler (*.ashx file),
    /// the bridge can be used to send such messages to their receivers inside the ASP.NET application.
    /// <example>
    /// The example shows receiving messages via the generic handler in ASP.NET service and
    /// using the bridge component to forward these messagas to receivers.
    /// <code>
    /// Instantiate the bridge in Global.asax.cs
    /// 
    ///  protected void Application_Start(object sender, EventArgs e)
    ///  {
    ///      // MessagingSystem that will be used by the Silverlight-ASP messaging "bridge"
    ///      myServerMessagingSystem = new SynchronousMessagingSystemFactory();
    ///      
    ///     // Create the duplex input channel for the broker.
    ///     // Note: The broker will listen to this channel.
    ///      IDuplexInputChannel aBrokerDuplexInputChannel = myServerMessagingSystem.CreateDuplexInputChannel("BrokerChannel");
    ///
    ///     // Create broker.
    ///     // Because we communicate with Silverlight it must be XmlSerialization.
    ///     IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory(new XmlStringSerializer());
    ///     myBroker = aBrokerFactory.CreateBroker();
    ///     myBroker.AttachDuplexInputChannel(aBrokerDuplexInputChannel);
    ///
    ///     // Create the duplex output channel for the client that will send notifications.
    ///     IDuplexOutputChannel aClientDuplexOutputChannel = myServerMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
    ///
    ///     // Create sender of notification messages.
    ///     myBrokerClient = aBrokerFactory.CreateBrokerClient();
    ///     myBrokerClient.AttachDuplexOutputChannel(aClientDuplexOutputChannel);
    ///
    ///     // Create bridge to connect Silverligh with Asp
    ///     IBridgeFactory aBridgeFactory = new BridgeFactory();
    ///     myBridge = aBridgeFactory.CreateDuplexBridge(myServerMessagingSystem, "BrokerChannel");
    ///
    ///     // Store the bridge to be used from MessagingHandler.ashx.cs
    ///     Application["Bridge"] = myBridge;
    ///     
    ///     .......
    ///  }     
    /// </code>
    /// 
    /// <code>
    /// Then the using the bridge to forward received messages:
    /// 
    /// // Handles messaging communication with Silverlight clients.
    /// public class MessagingHandler : IHttpHandler
    /// {
    ///     public void ProcessRequest(HttpContext context)
    ///     {
    ///         context.Application.Lock();
    /// 
    ///         // Get the bridge to the broker and forward the message to the messaging system
    ///         // connected with the bridge.
    ///         IDuplexBridge aBridge = context.Application["Bridge"] as IDuplexBridge;
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
    public class BridgeFactory : IBridgeFactory
    {
        /// <summary>
        /// Constructs the factory that will create bridges with default parameters.
        /// </summary>
        /// <remarks>
        /// Requester inactivity timeout is set to int.MaxValue and
        /// the maximum size of the response is int.MaxValue.
        /// </remarks>
        public BridgeFactory()
            : this(int.MaxValue, int.MaxValue)
        {
        }

        /// <summary>
        /// Constructs the factory with specified inactivity timeout and specified maximum size of the
        /// response message. 
        /// </summary>
        /// <remarks>
        /// If the response message should be bigger then the message will be sent on more times.
        /// This is to avoid that the sending of one big message will degradate the performance. <br/>
        /// These settings are valid only for the Duplex Bridge.
        /// </remarks>
        /// <param name="duplexRequesterInactivityTimeout">
        /// The inactivity timeout is used to recognize if the client is connected. If the client does not
        /// poll or send the message longer than the specified inactivity time, the client is considered to be disconnected.
        /// </param>
        /// <param name="maxSizeOfResponse">
        /// The maximum size of the response. E.g. If the bridge has collected 500 response messages which size is together 1MB
        /// and the maximum response size is set to 300KB, then the response will contain messages until the specified size is exceeded
        /// (including the first message that exceeds the limit). The messages that were not included in the response will be responded
        /// the next time.<br/>
        /// Notice, this parameter is applicable only for DuplexBridge.
        /// </param>
        public BridgeFactory(int duplexRequesterInactivityTimeout, int maxSizeOfResponse)
        {
            using (EneterTrace.Entering())
            {
                myDuplexRequesterInactivityTimeout = duplexRequesterInactivityTimeout;
                myMaxSizeOfResponse = maxSizeOfResponse;
            }
        }

        /// <summary>
        /// Creates the bridge to transfer one-way messages.
        /// </summary>
        /// <returns></returns>
        public IBridge CreateBridge()
        {
            using (EneterTrace.Entering())
            {
                return new Bridge();
            }
        }

        /// <summary>
        /// Creates the bridge to transfer messages in both directions (request-response).
        /// </summary>
        /// <param name="messagingSystemFactory"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public IDuplexBridge CreateDuplexBridge(IMessagingSystemFactory messagingSystemFactory, string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new DuplexBridge(channelId, messagingSystemFactory, myDuplexRequesterInactivityTimeout, myMaxSizeOfResponse);
            }
        }


        private int myDuplexRequesterInactivityTimeout;
        private int myMaxSizeOfResponse;
    }
}


#endif