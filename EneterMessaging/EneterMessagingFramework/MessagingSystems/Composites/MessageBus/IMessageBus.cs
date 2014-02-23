/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Message bus.
    /// </summary>
    /// <remarks>
    /// The message bus is the component that allows to dynamically expose various services.
    /// Services that want to be exposed via the message bus connect the message bus and register their service ids.
    /// Then, if a client wants to use the service it connects the message bus and asks for the particular service id.
    /// If such service id exists the message bus mediates the communication between the client and the service.<br/>
    /// <br/>
    /// The presence of the message bus is transparent for logic of services and their clients. The whole communication
    /// is realized via <see cref="MessageBusMessagingFactory"/> which ensures the interaction with the message bus.
    /// </remarks>
    public interface IMessageBus
    {
        /// <summary>
        /// The event is raised when a new service is registered.
        /// </summary>
		event EventHandler<MessageBusServiceEventArgs> ServiceRegistered;

        /// <summary>
        /// The event is raised when a service is unregistered.
        /// </summary>
		event EventHandler<MessageBusServiceEventArgs> ServiceUnregistered;

        /// <summary>
        /// Attaches duplex input channels that are used by clients and services to connect the message bus.
        /// </summary>
        /// <remarks>
        /// Once input channels are attached the message bus is listening and can be contacted by services and
        /// clients. <br/>
        /// <br/>
        /// To connect the message bus services must use 'Message Bus Duplex Input Channel' and clients must use
        /// 'Message Bus Duplex Output Channel'.<br/>
        /// <br/>
        /// IMPORTANT: Both duplex input channels must use the same protocol formatter!
        /// </remarks>
        /// <param name="serviceInputChannel">input channel used by services to register in the message bus.</param>
        /// <param name="clientInputChannel">input channel used by clients to connect a service via the message bus.</param>
        void AttachDuplexInputChannels(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel);

        /// <summary>
        /// Detaches input channels and stops the listening.
        /// </summary>
        void DetachDuplexInputChannels();

        /// <summary>
        /// Returns list of all connected services.
        /// </summary>
		IEnumerable<string> ConnectedServices { get; }

        /// <summary>
        /// Disconnect and unregisters the specified service.
        /// </summary>
        /// <param name="serviceAddress">id of the service that shall be unregistered</param>
		void DisconnectService(string serviceAddress);
    }
}
