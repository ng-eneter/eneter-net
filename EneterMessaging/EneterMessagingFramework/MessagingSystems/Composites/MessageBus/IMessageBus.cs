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
    /// Declares the message bus component.
    /// </summary>
    /// <remarks>
    /// The message bus is the component that exposes multiple services.
    /// When a service wants to expose its functionality via the message bus it connects the message bus and registers there.
    /// Then when a client wants to use the service it connects the message bus and asks for the service.
    /// If the requested service is registered the communication between the client and the service is mediated via the message bus.
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
        /// 'Message Bus Duplex Output Channel'. 
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
