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
    /// Declares service which can receive requests via RPC (Remote Procedure Call).
    /// </summary>
    /// <typeparam name="TServiceInterface">
    /// Service interface.
    /// The provided type must be a non-generic interface which can declare methods and events.
    /// </typeparam>
    /// <remarks>
    /// RpcService acts as a stub which provides the communication functionality for an instance implementing the given service interface.
    /// The provided service type must be an interface fulfilling following criteria:
    /// <ul>
    /// <li>Interface is not generic.</li>
    /// <li>Methods are not overloaded. It means there are no two methods with the same name.</li>
    /// <li>It can use events.</li>
    /// </ul>
    /// 
    /// <example>
    /// Declaring the service.
    /// <code>
    /// public interface IHello
    /// {
    ///     // Events work too.
    ///     event EventHandler&lt;MyEventArgs&gt; SomethingHappened;
    /// 
    ///     int Sum(int a, int b);
    /// 
    ///     void DoSomething();
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public interface IRpcService<TServiceInterface> : IAttachableDuplexInputChannel
        where TServiceInterface : class
    {
        /// <summary>
        /// Event raised when a client connected the service.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// Event raised when a client got disconnected from the service.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
    }
}