/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using System;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Extension providing the communication via the message bus.
    /// </summary>
    /// <remarks>
    /// This messaging wraps the communication with the message bus.
    /// The duplex input channel created by this messaging will automatically connect the message bus and register the service
    /// when the startListening() is called.<br/>
    /// The duplex output channel created by this messaging will automatically connect the message bus and ask for the service
    /// when the openConnection() is called.<br/>
    /// <br/>
    /// The following example shows how to communicate via the message bus.
    /// 
    /// <example>
    /// Implementation of the message bus service that will mediate the client-service communication:
    /// <code>
    /// class Program
    ///     {
    ///         static void Main(string[] args)
    ///         {
    ///             // Message Bus will use TCP for the communication.
    ///             IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    ///         
    ///             // Input channel to listen to services.
    ///             IDuplexInputChannel aServiceInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8045/");
    ///         
    ///             // Input channel to listen to clients.
    ///             IDuplexInputChannel aClientInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8046/");
    ///         
    ///             // Create the message bus.
    ///             IMessageBus aMessageBus = new MessageBusFactory().CreateMessageBus();
    ///         
    ///             // Attach channels to the message bus and start listening.
    ///             aMessageBus.AttachDuplexInputChannels(aServiceInputChannel, aClientInputChannel);
    ///         
    ///             Console.WriteLine("Message bus service is running. Press ENTER to stop.");
    ///             Console.ReadLine();
    ///         
    ///             // Detach channels and stop listening.
    ///             aMessageBus.DetachDuplexInputChannels();
    ///         }
    ///     }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Implementation of the service which is exposed via the message bus:
    /// <code>
    /// public interface IEcho
    /// {
    ///     string hello(string text);
    /// }
    /// 
    /// ....
    /// 
    /// internal class EchoService : IEcho
    /// {
    ///     public string hello(string text)
    ///     {
    ///         return text;
    ///     }
    /// }
    /// 
    /// ....
    /// 
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         // The service will communicate via Message Bus which is listening via TCP.
    ///         IMessagingSystemFactory aMessageBusUnderlyingMessaging = new TcpMessagingSystemFactory();
    ///         // note: only TCP/IP address which is exposed for services is needed.
    ///         IMessagingSystemFactory aMessaging = new MessageBusMessagingFactory("tcp://127.0.0.1:8045/", null, aMessageBusUnderlyingMessaging);
    ///     
    ///         // Create input channel listening via the message bus.
    ///         // Note: this is address of the service inside the message bus.
    ///         IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("Eneter.Echo");
    ///     
    ///         // Instantiate class implementing the service.
    ///         IEcho anEcho = new EchoService();
    ///     
    ///         // Create the RPC service.
    ///         IRpcService&lt;IEcho&gt; anEchoService = new RpcFactory().CreateService&lt;IEcho&gt;(anEcho);
    ///     
    ///         // Attach input channel to the service and start listening via the message bus.
    ///         anEchoService.AttachDuplexInputChannel(anInputChannel);
    ///     
    ///         Console.WriteLine("Echo service is running. Press ENTER to stop.");
    ///         Console.ReadLine();
    ///     
    ///         // Detach the input channel and stop listening.
    ///         anEchoService.DetachDuplexInputChannel();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Implementation of the client using the service which is exposed via the message bus:
    /// <code>
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         // The client will communicate via Message Bus which is listening via TCP.
    ///         IMessagingSystemFactory aMessageBusUnderlyingMessaging = new TcpMessagingSystemFactory();
    ///         // note: only TCP/IP address which is exposed for clients is needed. 
    ///         IMessagingSystemFactory aMessaging = new MessageBusMessagingFactory(null, "tcp://127.0.0.1:8046/", aMessageBusUnderlyingMessaging);
    ///     
    ///         // Create output channel that will connect the service via the message bus..
    ///         // Note: this is address of the service inside the message bus.
    ///         IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("Eneter.Echo");
    ///     
    ///         // Create the RPC client for the Echo Service.
    ///         IRpcClient&lt;IEcho&gt; aClient = new RpcFactory().CreateClient&lt;IEcho&gt;();
    ///     
    ///         // Attach the output channel and be able to communicate with the service via the message bus.
    ///         aClient.AttachDuplexOutputChannel(anOutputChannel);
    ///     
    ///         // Get the service proxy and call the echo method.
    ///         IEcho aProxy = aClient.Proxy;
    ///         String aResponse = aProxy.hello("hello");
    ///     
    ///         Console.WriteLine("Echo service returned: " + aResponse);
    ///         Console.WriteLine("Press ENTER to stop.");
    ///         Console.ReadLine();
    ///     
    ///         // Detach the output channel.
    ///         aClient.DetachDuplexOutputChannel();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class MessageBusMessagingFactory : IMessagingSystemFactory
    {
        private class MessageBusConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public MessageBusConnectorFactory(string serviceConnectingAddress, string clientConnectingAddress, ISerializer serializer, IMessagingSystemFactory messageBusMessaging)
            {
                using (EneterTrace.Entering())
                {
                    myClientConnectingAddress = clientConnectingAddress;
                    myServiceConnectingAddress = serviceConnectingAddress;
                    mySerializer = serializer;
                    myMessageBusMessaging = messageBusMessaging;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    // myClientConnectingAddress is address where message bus listens to connecting clients.
                    // outputConnectorAddress is responseReceiverId of connecting client.
                    IDuplexOutputChannel aMessageBusOutputChannel = myMessageBusMessaging.CreateDuplexOutputChannel(myClientConnectingAddress, outputConnectorAddress);
                    return new MessageBusOutputConnector(inputConnectorAddress, mySerializer, aMessageBusOutputChannel, OpenConnectionTimeout);
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    // myServiceConnectingAddress is address where message bus listen to connecting services.
                    // inputConnectorAddress is channelId of connecting service.
                    IDuplexOutputChannel aMessageBusOutputChannel = myMessageBusMessaging.CreateDuplexOutputChannel(myServiceConnectingAddress, inputConnectorAddress);
                    return new MessageBusInputConnector(mySerializer, aMessageBusOutputChannel);
                }
            }

            public TimeSpan OpenConnectionTimeout { get; set; }

            private string myClientConnectingAddress;
            private string myServiceConnectingAddress;
            private ISerializer mySerializer;
            private IMessagingSystemFactory myMessageBusMessaging;
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="serviceConnctingAddress">message bus address for registered services.</param>
        /// <param name="clientConnectingAddress">message bus address for clients that want to connect a registered service.</param>
        /// <param name="underlyingMessaging">messaging system used by the message bus.</param>
        public MessageBusMessagingFactory(string serviceConnctingAddress, string clientConnectingAddress, IMessagingSystemFactory underlyingMessaging)
            : this(serviceConnctingAddress, clientConnectingAddress, underlyingMessaging, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="serviceConnctingAddress">message bus address for registered services.</param>
        /// <param name="clientConnectingAddress">message bus address for clients that want to connect a registered service.</param>
        /// <param name="underlyingMessaging">messaging system used by the message bus.</param>
        /// <param name="protocolFormatter">protocol formatter used for the communication between channels.</param>
        public MessageBusMessagingFactory(string serviceConnctingAddress, string clientConnectingAddress, IMessagingSystemFactory underlyingMessaging, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myConnectorFactory = new MessageBusConnectorFactory(serviceConnctingAddress, clientConnectingAddress, serializer, underlyingMessaging);
                myConnectorFactory.OpenConnectionTimeout = TimeSpan.FromMilliseconds(30000);

                // Dispatch events in the same thread as notified from the underlying messaging.
                OutputChannelThreading = new NoDispatching();
                InputChannelThreading = OutputChannelThreading;
            }
        }

        /// <summary>
        /// Creates duplex output channel.
        /// </summary>
        /// <remarks>
        /// Channel id represents the logical address of the service inside the message bus. E.g. "Eneter.EchoService".
        /// </remarks>
        /// <param name="channelId">logical address of the service inside the message bus</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex output channel.
        /// </summary>
        /// <remarks>
        /// Channel id represents the logical address of the service inside the message bus. E.g. "Eneter.EchoService".
        /// </remarks>
        /// <param name="channelId">logical address of the service inside the message bus</param>
        /// <param name="responseReceiverId">unique id representing the client</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex input channel.
        /// </summary>
        /// <remarks>
        /// Channel id represents the logical address of the service inside the message bus. E.g. "Eneter.EchoService".
        /// </remarks>
        /// <param name="channelId">logical address of the service inside the message bus</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IInputConnector anInputConnector = myConnectorFactory.CreateInputConnector(channelId);
                DefaultDuplexInputChannel anInputChannel = new DefaultDuplexInputChannel(channelId, aDispatcher, aDispatcherAfterMessageDecoded, anInputConnector);
                return anInputChannel;
            }
        }


        /// <summary>
        /// Provides thread dispatcher responsible for routing events from duplex input channel according to
        /// desired threading model.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed one by one via a working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Provides thread dispatcher responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed one by one via a working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        public TimeSpan OpenConnectionTimeout
        {
            get { return myConnectorFactory.OpenConnectionTimeout; }
            set { myConnectorFactory.OpenConnectionTimeout = value; }
        }


        private MessageBusConnectorFactory myConnectorFactory;
        private IThreadDispatcherProvider myDispatchingAfterMessageDecoded = new SyncDispatching();
    }
}
