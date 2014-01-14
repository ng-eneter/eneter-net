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
    /// Declares client using remote procedure calls.
    /// </summary>
    /// <typeparam name="TServiceInterface">Service interface.</typeparam>
    public interface IRpcClient<TServiceInterface> : IAttachableDuplexOutputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// Event raised when the connection with the service was open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// Event raised when the connection with the service was closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

#if !SILVERLIGHT && !COMPACT_FRAMEWORK
        /// <summary>
        /// Returns the proxy for the service.
        /// </summary>
        /// <remarks>
        /// The returned instance provides the proxy for the service interface.
        /// Calling of a method from the proxy will result to the communication with the service.
        /// </remarks>
        TServiceInterface Proxy { get; }
#endif

        /// <summary>
        /// Subscribes to an event from the service.
        /// </summary>
        /// <typeparam name="TEventArgs">Type of the event args. It must be derived from EventArgs.</typeparam>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler processing the event</param>
        /// <remarks>
        /// Use this method if subscribing via the proxy is not suitable. (E.g. the proxy is not supported in Silverlight)
        /// If the method does not exist in the service interface the exception is thrown.
        /// </remarks>
        void SubscribeRemoteEvent<TEventArgs>(string eventName, EventHandler<TEventArgs> eventHandler) where TEventArgs : EventArgs;

        /// <summary>
        /// Unsubscribes from the event in the service.
        /// </summary>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler that shall be unsubscribed</param>
        /// <remarks>
        /// Use this method if subscribing via the proxy is not suitable. (E.g. the proxy is not supported in Silverlight)
        /// If the method does not exist in the service interface the exception is thrown.
        /// </remarks>
        void UnsubscribeRemoteEvent(string eventName, Delegate eventHandler);


        /// <summary>
        /// Calls the method in the service.
        /// </summary>
        /// <param name="methodName">name of the method</param>
        /// <param name="args">list of arguments</param>
        /// <returns>returned value. null if it returns 'void'</returns>
        /// <remarks>
        /// Use this method if subscribing via the proxy is not suitable. (E.g. the proxy is not supported in Silverlight)
        /// If the method does not exist in the service interface the exception is thrown.
        /// </remarks>
        object CallRemoteMethod(string methodName, params object[] args);
    }
}