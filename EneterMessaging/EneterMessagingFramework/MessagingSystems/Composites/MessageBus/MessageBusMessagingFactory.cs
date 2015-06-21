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
    /// This messaging provides the client-service communication via the message bus.
    /// It ensures the communication via the message bus is transparent and for communicating parts it looks like a normal
    /// communication via output and input channel.<br/>
    /// The duplex input channel created by this messaging will automatically connect the message bus and register the service
    /// when the StartListening() is called.<br/>
    /// The duplex output channel created by this messaging will automatically connect the message bus and ask for the service
    /// when the OpenConnection() is called.<br/>
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
        private class MessageBusOutputConnectorFactory : IOutputConnectorFactory
        {
            public MessageBusOutputConnectorFactory(string clientConnectingAddress, ISerializer serializer)
            {
                using (EneterTrace.Entering())
                {
                    myClientConnectingAddress = clientConnectingAddress;
                    mySerializer = serializer;
                }
            }

            public TimeSpan OpenConnectionTimeout { get; set; }

            public IMessagingSystemFactory ClientMessaging { get; set; }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    // myClientConnectingAddress is address where message bus listens to connecting clients.
                    // outputConnectorAddress is responseReceiverId of connecting client.
                    IDuplexOutputChannel aMessageBusOutputChannel = ClientMessaging.CreateDuplexOutputChannel(myClientConnectingAddress, outputConnectorAddress);
                    return new MessageBusOutputConnector(inputConnectorAddress, mySerializer, aMessageBusOutputChannel, (int)OpenConnectionTimeout.TotalMilliseconds);
                }
            }

            private string myClientConnectingAddress;
            private ISerializer mySerializer;
        }

        private class MessageBusInputConnectorFactory : IInputConnectorFactory
        {
            public MessageBusInputConnectorFactory(string serviceConnectingAddress, ISerializer serializer)
            {
                using (EneterTrace.Entering())
                {
                    myServiceConnectingAddress = serviceConnectingAddress;
                    mySerializer = serializer;
                }
            }

            public IMessagingSystemFactory ServiceMessaging { get; set; }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    // myServiceConnectingAddress is address where message bus listen to connecting services.
                    // inputConnectorAddress is channelId of connecting service.
                    IDuplexOutputChannel aMessageBusOutputChannel = ServiceMessaging.CreateDuplexOutputChannel(myServiceConnectingAddress, inputConnectorAddress);
                    return new MessageBusInputConnector(mySerializer, aMessageBusOutputChannel);
                }
            }

            private string myServiceConnectingAddress;
            private ISerializer mySerializer;
        }


        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <remarks>
        /// Message bus instantiated using this constructor will use MessageBusCustom serializer which is optimized to serialize/deserialize
        /// only MessageBusMessage used for the communication with the message bus.
        /// </remarks>
        /// <param name="serviceConnctingAddress">message bus address intended for services which want to register in the message bus.
        /// It can be null if the message bus factory is intended to create only duplex output channels.</param>
        /// <param name="clientConnectingAddress">clientConnectingAddress message bus address intended for clients which want to connect a registered service.
        /// It can be null if the message bus factory is intended to create only duplex input channels.</param>
        /// <param name="underlyingMessaging">messaging system used by the message bus.</param>
        public MessageBusMessagingFactory(string serviceConnctingAddress, string clientConnectingAddress, IMessagingSystemFactory underlyingMessaging)
            : this(serviceConnctingAddress, clientConnectingAddress, underlyingMessaging, new MessageBusCustomSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="serviceConnectingAddress">message bus address intended for services which want to register in the message bus.
        /// It can be null if the message bus factory is intended to create only duplex output channels.</param>
        /// <param name="clientConnectingAddress">clientConnectingAddress message bus address intended for clients which want to connect a registered service.
        /// It can be null if the message bus factory is intended to create only duplex input channels.</param>
        /// <param name="underlyingMessaging">messaging system used by the message bus.</param>
        /// <param name="serializer">serializer which is used to serialize {@link MessageBusMessage} which is internally used for the communication with
        /// the message bus.</param>
        public MessageBusMessagingFactory(string serviceConnectingAddress, string clientConnectingAddress, IMessagingSystemFactory underlyingMessaging, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myOutputConnectorFactory = new MessageBusOutputConnectorFactory(clientConnectingAddress, serializer);
                myInputConnectorFactory = new MessageBusInputConnectorFactory(serviceConnectingAddress, serializer);

                ClientMessaging = underlyingMessaging;
                ServiceMessaging = underlyingMessaging;

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
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aDispatcherAfterMessageDecoded, myOutputConnectorFactory);
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
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aDispatcherAfterMessageDecoded, myOutputConnectorFactory);
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
                IInputConnector anInputConnector = myInputConnectorFactory.CreateInputConnector(channelId);
                DefaultDuplexInputChannel anInputChannel = new DefaultDuplexInputChannel(channelId, aDispatcher, aDispatcherAfterMessageDecoded, anInputConnector);
                return anInputChannel;
            }
        }

        /// <summary>
        /// Get or set the messaging used by clients to connect the message bus.
        /// </summary>
        public IMessagingSystemFactory ClientMessaging
        {
            get { return myOutputConnectorFactory.ClientMessaging; }
            set { myOutputConnectorFactory.ClientMessaging = value; }
        }

        /// <summary>
        /// Get or set the messaging used by services to be exposed via the message bus.
        /// </summary>
        public IMessagingSystemFactory ServiceMessaging
        {
            get { return myInputConnectorFactory.ServiceMessaging; }
            set { myInputConnectorFactory.ServiceMessaging = value; }
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

        /// <summary>
        /// Maximum time for opening connection with the service via the message bus. Default value is 30 seconds.
        /// </summary>
        /// <remarks>
        /// When the client opens the connection with a service via message bus it requests message bus to open connection
        /// with a desired service. The message checks if the requested service exists and if yes it forwards the open connection request.
        /// Then when the service receives the open connection request it sends back the confirmation message that the client is connected.
        /// This timeout specifies the maximum time which is allowed for sending the open connection request and receiving the confirmation from the service.
        /// </remarks>
        public TimeSpan ConnectTimeout
        {
            get { return myOutputConnectorFactory.OpenConnectionTimeout; }
            set { myOutputConnectorFactory.OpenConnectionTimeout = value; }
        }


        private MessageBusOutputConnectorFactory myOutputConnectorFactory;
        private MessageBusInputConnectorFactory myInputConnectorFactory;
        private IThreadDispatcherProvider myDispatchingAfterMessageDecoded = new SyncDispatching();
    }
}
