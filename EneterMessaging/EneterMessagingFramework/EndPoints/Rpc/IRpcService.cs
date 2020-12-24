


using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Service which exposes the interface for Remote Procedure Call (note: it also works with .NET).
    /// </summary>
    /// <typeparam name="TServiceInterface">Service interface.</typeparam>
    /// <remarks>
    /// RpcService acts as a stub which provides the communication functionality for an instance implementing the given service interface.<br/>
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
    /// 
    /// <example>
    /// The following example shows how to declare interface that can be used for Java/C# communication.
    /// <code>
    /// // C# interface
    /// public interface IMyInterface
    /// {
    ///    // Event without arguments.
    ///    event EventHandler SomethingHappened;
    ///    
    ///    // Event with arguments.
    ///    event EventHandler&lt;MyEventArgs&gt; SomethingElseHappened;
    ///    
    ///    // Simple method.
    ///    void DoSomething();
    ///    
    ///    // Method with arguments.
    ///    int Calculate(int a, int b);
    /// }
    /// </code>
    /// 
    /// <code>
    /// // Java equivalent
    /// // Note: Names of methods and events must be same. So e.g. if the interface is declared in .NET with
    /// //       then you may need to start method names with Capital.
    /// public interface IMyInterface
    /// {
    ///    // Event without arguments.
    ///    Event&lt;EventArgs&gt; SomethingHappened();
    ///    
    ///    // Event with arguments.
    ///    Event&lt;MyArgs&gt; SomethingElseHappened();
    ///    
    ///    // Simple method.
    ///    void DoSomething();
    ///    
    ///    // Method with arguments.
    ///    int Calculate(int a, int b);
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