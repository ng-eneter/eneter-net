﻿/*
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
    /// The message bus is the component which can expose services.
    /// The service connects the message bus and registers its service id.
    /// When a client needs to use the service it connects the message bus and specifies the service id.
    /// If the service id exists the message bus establishes the connection between the client and the service.<br/>
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
        /// Attaches input channels which are used for the communication with the message bus.
        /// </summary>
        /// <remarks>
        /// Once input channels are attached the message bus is listening and can be used by services and
        /// clients.
        /// </remarks>
        /// <param name="serviceInputChannel">channel used by services for registering their services in the message bus</param>
        /// <param name="clientInputChannel">channel used by clients for connecting services via the message bus.</param>
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
