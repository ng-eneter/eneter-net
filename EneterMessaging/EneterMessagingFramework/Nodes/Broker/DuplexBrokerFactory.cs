/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Infrastructure.ConnectionProvider;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Nodes.ChannelWrapper;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Implents the factory creating broker and broker client.
    /// </summary>
    /// <remarks>
    /// The broker is the component that provides functionality for publish-subscribe scenarios.
    /// IDuplexBrokerClient provides functionality to send notification messages to the broker
    /// and also to subscribe for desired messages.
    /// 
    /// <example>
    /// The example shows how to create and use the broker communicating via TCP.
    /// <code>
    /// // Create Tcp based messaging.
    /// IMessagingSystemFactory aMessagingFactory = new TcpMessagingSystemFactory();
    /// 
    /// // Create duplex input channel listening to messages.
    /// IDuplexInputChannel anInputChannel = aMessagingFactory.CreateDuplexInputChannel("tcp://127.0.0.1:7980/");
    /// 
    /// // Create the factory for the broker.
    /// IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
    /// 
    /// // Create the broker.
    /// IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
    /// 
    /// // Attach the Tcp duplex input channel to the broker and start listening.
    /// aBroker.AttachDuplexInputChannel(anInputChannel);
    /// </code>
    /// 
    /// <code>
    /// Subscribing for the notification messages.
    /// 
    /// // Create Tcp based messaging for the silverlight client.
    /// IMessagingSystemFactory aTcpMessagingFactory = new TcpMessagingSystemFactory();
    /// 
    /// // Create duplex output channel to send and receive messages.
    /// myOutputChannel = aTcpMessagingFactory.CreateDuplexOutputChannel("tcp://127.0.0.1:7980/");
    /// 
    /// // Create the broker client
    /// IDuplexBrokerFactory aDuplexBrokerFactory = new DuplexBrokerFactory();
    /// myBrokerClient = aDuplexBrokerFactory.CreateBrokerClient();
    /// 
    /// // Handler to process notification messages.
    /// myBrokerClient.BrokerMessageReceived += NotifyMessageReceived;
    /// 
    /// // Attach the channel to the broker client to be able to send and receive messages.
    /// myBrokerClient.AttachDuplexOutputChannel(myOutputChannel);
    /// 
    /// // Subscribe in broker to receive chat messages.
    /// myBrokerClient.Subscribe("MyChatMessageType");
    /// 
    /// 
    /// ...
    /// 
    /// 
    /// // Send message to the broker. The broker will then forward it to all subscribers.
    /// XmlStringSerializer anXmlSerializer = new XmlStringSerializer();
    /// object aSerializedChatMessage = anXmlSerializer.Serialize&lt;ChatMessage&gt;(aChatMessage);
    /// myBrokerClient.SendMessage("MyChatMessageType", aSerializedChatMessage);
    /// </code>
    /// </example>
    /// </remarks>
    public class DuplexBrokerFactory : IDuplexBrokerFactory
    {
        /// <summary>
        /// Constructs the broker factory with XmlStringSerializer.
        /// </summary>
        public DuplexBrokerFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the broker factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer used by the broker</param>
        public DuplexBrokerFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                IsPublisherNotified = true;
                Serializer = serializer;
            }
        }

        /// <summary>
        /// Creates the broker client.
        /// </summary>
        /// <remarks>
        /// The broker client is able to send messages to the broker (via attached duplex output channel).
        /// It also can subscribe for messages to receive notifications from the broker.
        /// </remarks>
        public IDuplexBrokerClient CreateBrokerClient()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexBrokerClient(Serializer);
            }
        }

        /// <summary>
        /// Creates the broker.
        /// </summary>
        /// <remarks>
        /// The broker receives messages and forwards them to subscribers.
        /// </remarks>
        public IDuplexBroker CreateBroker()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexBroker(IsPublisherNotified, Serializer);
            }
        }

        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Indicates whether the BrokerClient shall be notified about own events.
        /// </summary>
        /// <remarks>
        /// It allows to specify if the broker client gets notification from the broker for its own published events.
        /// E.g. if the broker client is subscribed to the event 'StatusChanged' and if this broker client also publishes the
        /// event 'StatusChanged' then if the parmater publisherCanBeNotified is false the broker client will not get notification events
        /// from its own published events.
        /// </remarks>
        public bool IsPublisherNotified { get; set; }
    }
}
