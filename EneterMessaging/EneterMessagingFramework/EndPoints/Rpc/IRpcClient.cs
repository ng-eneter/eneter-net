/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Client which can use Remote Procedure Calls (note: it also works with Java and Android).
    /// </summary>
    /// <remarks>
    /// RpcClient acts as a proxy providing the communication functionality allowing a client to call methods exposed by the service.
    /// </remarks>
    /// <typeparam name="TServiceInterface">Interface exposed by the service.</typeparam>
    public interface IRpcClient<TServiceInterface> : IAttachableDuplexOutputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// Event raised when the connection with the service is open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// Event raised when the connection with the service is closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

#if !NETSTANDARD
        /// <summary>
        /// Returns service proxy instance.
        /// </summary>
        TServiceInterface Proxy { get; }
#endif

        /// <summary>
        /// Subscribes to an event from the service.
        /// </summary>
        /// <typeparam name="TEventArgs">Type of the event args. It must be derived from EventArgs.</typeparam>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler processing the event</param>
        /// <remarks>
        /// You can use this method for subscribing if you do not want to use the service proxy.
        /// </remarks>
        void SubscribeRemoteEvent<TEventArgs>(string eventName, EventHandler<TEventArgs> eventHandler) where TEventArgs : EventArgs;

        /// <summary>
        /// Unsubscribes from the event in the service.
        /// </summary>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler that shall be unsubscribed</param>
        /// <remarks>
        /// You can use this method for unsubscribing if you do not want to use the service proxy.
        /// </remarks>
        void UnsubscribeRemoteEvent(string eventName, Delegate eventHandler);


        /// <summary>
        /// Calls a method in the service.
        /// </summary>
        /// <param name="methodName">name of the method that shall be called.</param>
        /// <param name="args">list of arguments</param>
        /// <returns>returned value. null if it returns 'void'</returns>
        /// <remarks>
        /// You can use this method if you do not want to use the service proxy.
        /// </remarks>
        object CallRemoteMethod(string methodName, params object[] args);
    }
}