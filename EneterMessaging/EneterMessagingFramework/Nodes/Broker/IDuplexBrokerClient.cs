/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Publishes and subscribes messages in the broker.
    /// </summary>
    /// <remarks>
    /// The broker client is the component which interacts with the broker.
    /// It allows to publish messages via the broker and to subscribe for desired messages in the broker.
    /// <example>
    /// The following example shows how to subscribe a message in the broker:
    /// <code>
    /// // Create the broker client.
    /// IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
    /// IDuplexBrokerClient aBrokerClient = aBrokerFactory.CreateBrokerClient();
    /// 
    /// // Register handler to process subscribed messages from the broker.
    /// aBrokerClient.BrokerMessageReceived += OnBrokerMessageReceived;
    /// 
    /// // Attach output channel and be able to communicate with the broker.
    /// // E.g. if the broker communicates via TCP.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:9843/");
    /// aBrokerClient.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// // Now when the connection with the broker is establish so we can subscribe for
    /// // messages in the broker.
    /// // After this call whenever somebody sends the message type 'MyMessageType' into the broker
    /// // the broker will forward it to this broker client and the message handler
    /// // myMessageHandler will be called.
    /// aBrokerClient.Subscribe("MyMessageType");
    /// </code>
    /// The following example shows how to publish a message via the broker:
    /// <code>
    /// // Create the broker client.
    /// IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
    /// IDuplexBrokerClient aBrokerClient = aBrokerFactory.CreateBrokerClient();
    /// 
    /// // Attach output channel and be able to communicate with the broker.
    /// // E.g. if the broker communicates via TCP.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:9843/");
    /// aBrokerClient.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// // Now when the connection with the broker is establish so we can publish the message.
    /// // Note: the broker will receive the message and will forward it to everybody who is subscribed for MyMessageType.
    /// aBrokerClient.SendMessage("MyMessageType", "Hello world.");
    /// </code>
    /// </example>
    /// 
    /// 
    /// 
    /// </remarks>
    public interface IDuplexBrokerClient : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// Event raised when the connection with the service was open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// Event raised when the connection with the service was closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is invoked when the observed event is received from the broker.
        /// </summary>
        event EventHandler<BrokerMessageReceivedEventArgs> BrokerMessageReceived;

        /// <summary>
        /// Publishes the message.
        /// </summary>
        /// <param name="messageType">identifies the type of the published message. The broker will forward the message
        /// to all subscribers subscribed to this message type.</param>
        /// <param name="serializedMessage">message content.</param>
        void SendMessage(string messageType, object serializedMessage);

        /// <summary>
        /// Subscribes for the message type.
        /// </summary>
        /// <param name="messageType">identifies the type of the message which shall be subscribed.</param>
        void Subscribe(string messageType);

        /// <summary>
        /// Subscribes for list of message types.
        /// </summary>
        /// <param name="messageTypes">list of message types which shall be subscribed.</param>
        void Subscribe(string[] messageTypes);

        /// <summary>
        /// Unsubscribes from the specified message type.
        /// </summary>
        /// <param name="messageType">message type the client does not want to receive anymore.</param>
        void Unsubscribe(string messageType);

        /// <summary>
        /// Unsubscribes from specified events.
        /// </summary>
        /// <param name="messageTypes">list of message types the client does not want to receive anymore.</param>
        void Unsubscribe(string[] messageTypes);

        /// <summary>
        /// Unsubscribe all messages.
        /// </summary>
        void Unsubscribe();
    }
}
