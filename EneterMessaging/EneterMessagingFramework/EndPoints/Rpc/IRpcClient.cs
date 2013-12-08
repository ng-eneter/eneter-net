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
    /// Declares client providing communication using remote procedure calls.
    /// </summary>
    /// <typeparam name="TServiceInterface">Service interface containing methods and events.</typeparam>
    public interface IRpcClient<TServiceInterface> : IAttachableDuplexOutputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// The event is raised when the connection with the service was open.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the service was closed.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

#if !SILVERLIGHT
        /// <summary>
        /// Returns the instance of the service interface implementing the proxy.
        /// </summary>
        /// <remarks>
        /// The returned instance inmplements the proxy ensuring the communication with the service.
        /// </remarks>
        TServiceInterface Proxy { get; }
#endif

        /// <summary>
        /// Subscribes for the event on the service.
        /// </summary>
        /// <typeparam name="TEventArgs">Type of the event args. It must be derived from EventArgs.</typeparam>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler processing the event</param>
        /// <remarks>
        /// Prefer to use the Proxy property where you can subscribe to events exposed via the interface.
        /// Use this method in cases when the proxy is not available (e.g. in Silverloight).
        /// </remarks>
        void SubscribeRemoteEvent<TEventArgs>(string eventName, Action<object, TEventArgs> eventHandler) where TEventArgs : EventArgs;

        /// <summary>
        /// Unsubscribe from the event on the service.
        /// </summary>
        /// <param name="eventName">name of the event</param>
        /// <param name="eventHandler">event handler that shall be unsubscribed</param>
        /// <remarks>
        /// Prefer to use the Proxy property where you can unsubscribe from events exposed via the interface.
        /// Use this method in cases when the proxy is not available (e.g. in Silverloight).
        /// </remarks>
        void UnsubscribeRemoteEvent(string eventName, Delegate eventHandler);


        /// <summary>
        /// Calls the remote method on the service.
        /// </summary>
        /// <param name="methodName">name of the method</param>
        /// <param name="args">list of arguments</param>
        /// <returns>returned value. null if it returns 'void'</returns>
        object CallRemoteMethod(string methodName, params object[] args);
    }
}
