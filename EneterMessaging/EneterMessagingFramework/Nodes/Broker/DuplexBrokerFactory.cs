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
    /// Delegate is called when broker receives the request to Publish, Subscribe or Unsubscribe from BrokerDuplexClient.
    /// </summary>
    /// <param name="responseReceiverId">id of client which sent the request.</param>
    /// <param name="request">request sent by the client.</param>
    /// <returns>true if the request is valid, false if the request is invalid. If it returns false the request
    /// will not be invoked and the client will be disconnected.
    /// </returns>
    public delegate bool AuthorizeBrokerRequestCallback(string responseReceiverId, BrokerMessage request);


    /// <summary>
    /// Creates broker and broker client.
    /// </summary>
    /// <remarks>
    /// The broker is the component for publish-subscribe scenarios. It maintains the list of subscribers.
    /// When it receives a notification message it forwards it to subscribed clients. 
    /// IDuplexBrokerClient can publish messages via the broker
    /// and also can subscribe for desired messages.
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
        /// Constructs the broker factory with optimized custom serializer.
        /// </summary>
        public DuplexBrokerFactory()
            : this(new BrokerCustomSerializer())
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
                return new DuplexBroker(IsPublisherNotified, Serializer, BrokerRequestAuthorizer);
            }
        }

        /// <summary>
        /// Serializer to serialize/deserialize <see cref="BrokerMessage"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="BrokerMessage"/> is used for the communication with the broker.
        /// </remarks>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Gets/sets callback for retrieving serializer based on response receiver id.
        /// </summary>
        /// <remarks>
        /// This callback is used by DuplexBroker when it needs to serialize/deserialize the BrokerMessage from a DuplexBrokerClient.
        /// Providing this callback allows to use a different serializer for each connected DuplexBrokerClient.
        /// This can be used e.g. if the communication with each DuplexBrokerClient needs to be encrypted using a different password.<br/>
        /// <br/>
        /// The default value is null and it means SerializerProvider callback is not used and one serializer which specified in the Serializer property is used for all serialization/deserialization.<br/>
        /// If SerializerProvider is not null then the setting in the Serializer property is ignored.
        /// </remarks>
        public GetSerializerCallback SerializerProvider
        {
            get
            {
                if (Serializer is CallbackSerializer)
                {
                    return ((CallbackSerializer)Serializer).GetSerializerCallback;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    if (Serializer is CallbackSerializer)
                    {
                        Serializer = null;
                    }
                }
                else
                {
                    Serializer = new CallbackSerializer(value);
                }
            }
        }

        /// <summary>
        /// Gets/sets callback for authorizing request messages received from DuplexBrokerClient.
        /// </summary>
        /// <remarks>
        /// DuplexBrokerClient can send request messages for publishing, subscribing or unsubscribing of messages.
        /// If the callback is not null it is called whenever such request is received from DuplexBrokerClient.
        /// If the callback returns true the request is considered authorized and the broker will perform it.
        /// If the callback returns false the request is considered invalid and will not be performed and DuplexBrokerClient
        /// will be disconnected.
        /// E.g. if a DuplexBrokerClient asks broker to publish a message and the callback returns false the message will not be sent
        /// to subscribers and DuplexBrokerClient will be disconnected.
        /// </remarks>
        public AuthorizeBrokerRequestCallback BrokerRequestAuthorizer { get; set; }

        /// <summary>
        /// Sets the flag whether the publisher which sent a message shall be notified in case it is subscribed to the same message.
        /// </summary>
        /// <remarks>
        /// When a DuplexBrokerClient sent a message the broker forwards the message to all subscribed DuplexBrokerClients.
        /// In case the DuplexBrokerClient is subscribed to the same message the broker will notify it if the flag
        /// IsBublisherNotified is set to true.
        /// If it is set to false then the broker will not forward the message to the DuplexBrokerClient which
        /// published the message.
        /// </remarks>
        public bool IsPublisherNotified { get; set; }
    }
}
