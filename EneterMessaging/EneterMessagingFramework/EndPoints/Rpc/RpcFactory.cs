/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Creates services and clients that can communicate using RPC (Remote Procedure Calls).
    /// </summary>
    /// <remarks>
    /// RPC is the communication scenario where an application (typically client) executes a method in another application (typically service). 
    /// RpcFactory provides methods to instantiate RpcService and RpcClient objects.
    /// 
    /// RpcService acts as a stub which provides the communication functionality allowing the service to be reached from outside.
    /// RpcClient acts as a proxy which provides the communication functionality allowing the client to call remote methods in the service.
    /// 
    /// The following example shows simple client-service communication using RPC.
    /// 
    /// <example>
    /// Implementing the service:
    /// <code>
    /// public interface IHello
    /// {
    ///     event EventHandler&lt;MyEventArgs&gt; SomethingHappned;
    ///     int Calculate(int a, int b);
    /// }
    /// 
    /// public class HelloService : IHello
    /// {
    ///     public event EventHandler&lt;MyEventArgs&gt; SomethingHappned;
    /// 
    ///     int Calculate(int a, int b)
    ///     {
    ///         return a + b;
    ///     }
    /// 
    ///     public void RaiseEvent()
    ///     {
    ///         if (SomethingHappned != null)
    ///         {
    ///             SomethingHappned(this, new MyEventArgs());
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         // Instantiate service.
    ///         HelloService aHelloService = new HelloService();
    ///         IRpcFactory anRpcFactory = new RpcFactory();
    ///         IRpcService&lt;IHello&gt; aService = anRpcFactory.CreateService&lt;ICalculator&gt;(aHelloService);
    /// 
    ///         // Attach input channel and start listening.
    ///         IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    ///         IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8045/");
    ///         aService.AttachDuplexInputChannel(anInputChannel);
    /// 
    ///         Console.WriteLine("Hello service started. Press ENTER to stop.");
    ///         Console.ReadLine();
    /// 
    ///         // Detach input channel and stop listening.
    ///         // Note: it releases the listening thread.
    ///         aService.DetachDuplexInputChannel();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Using service from the client.
    /// <code>
    /// // Get the service proxy for the interface.
    /// IRpcFactory anRpcFactory = new RpcFactory();
    /// myRpcClient = anRpcFactory.CreateClient&lt;IHello&gt;();
    /// 
    /// // Attach output channel and be able to communicate.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8045/");
    /// myRpcClient.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// // Call service.
    /// IHello aServiceProxy = myRpcClient.Proxy;
    /// int aResult = aServiceProxy.Calculate(10, 20);
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    public class RpcFactory : IRpcFactory
    {
        /// <summary>
        /// Constructs RpcFactory with default <see cref="XmlStringSerializer"/>.
        /// </summary>
        public RpcFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs RpcFactory with specified serializer.
        /// </summary>
        /// <remarks>
        /// List of serializers provided by Eneter: <see cref="Eneter.Messaging.DataProcessing.Serializing"/>.
        /// </remarks>
        /// <param name="serializer"></param>
        public RpcFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                Serializer = serializer;
                SerializerProvider = null;

                // Default timeout is set to infinite by default.
                RpcTimeout = TimeSpan.FromMilliseconds(-1);

                RpcClientThreading = new SyncDispatching();
            }
        }

        /// <summary>
        /// Creates RPC client for the given interface.
        /// </summary>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <returns>RpcClient instance</returns>
        public IRpcClient<TServiceInterface> CreateClient<TServiceInterface>() where TServiceInterface : class
        {
            using (EneterTrace.Entering())
            {
                return new RpcClient<TServiceInterface>(Serializer, RpcTimeout, RpcClientThreading.GetDispatcher());
            }
        }

        /// <summary>
        /// Creates single-instance RPC service for the given interface.
        /// </summary>
        /// <remarks>
        /// Single-instance means that there is one instance of the service which shared by all clients.
        /// </remarks>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <param name="service">instance implementing the given service interface</param>
        /// <returns>RpcService instance.</returns>
        public IRpcService<TServiceInterface> CreateSingleInstanceService<TServiceInterface>(TServiceInterface service) where TServiceInterface : class
        {
            using (EneterTrace.Entering())
            {
#if !COMPACT_FRAMEWORK20
                return new RpcService<TServiceInterface>(service, Serializer, SerializerProvider);
#else
                throw new NotSupportedException("RPC service is not supported in Compact Framework 2.0.");
#endif
            }
        }

        /// <summary>
        /// Creates per-client-instance RPC service for the given interface.
        /// </summary>
        /// <remarks>
        /// Per-client-instance means that for each connected client is created a separate instace of the service.
        /// </remarks>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <param name="serviceFactoryMethod">factory method used to create the service instance when the client is connected</param>
        /// 
        /// <returns></returns>
        public IRpcService<TServiceInterface> CreatePerClientInstanceService<TServiceInterface>(Func<TServiceInterface> serviceFactoryMethod) where TServiceInterface : class
        {
            using (EneterTrace.Entering())
            {
#if !COMPACT_FRAMEWORK20
                return new RpcService<TServiceInterface>(serviceFactoryMethod, Serializer, SerializerProvider);
#else
                throw new NotSupportedException("RPC service is not supported in Compact Framework 2.0.");
#endif
            }
        }

        /// <summary>
        /// Gets/sets serializer used for serializing messages between RpcClient and RpcService.
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Gets/sets callback for retrieving serializer based on response receiver id.
        /// </summary>
        /// <remarks>
        /// This callback is used by RpcService when it needs to serialize/deserialize the communication with RpcClient.
        /// Providing this callback allows to use a different serializer for each connected client.
        /// This can be used e.g. if the communication with each client needs to be encrypted by a differently.<br/>
        /// <br/>
        /// The default value is null and it means the serializer specified in the Serializer property is used for all serialization/deserialization.
        /// </remarks>
        public GetSerializerCallback SerializerProvider { get; set; }

        /// <summary>
        /// Gets/sets threading mechanism used for invoking events (if RPC interface has some) and ConnectionOpened and ConnectionClosed events.
        /// </summary>
        /// <remarks>
        /// Default setting is that events are routed one by one via a working thread.<br/>
        /// It is recomended not to set the same threading mode for the attached output channel because a deadlock can occur when
        /// a remote procedure is called (e.g. if a return value from a remote method is routed to the same thread as is currently waiting for that return value the deadlock occurs).<br/>
        /// <br/>
        /// Note: The threading mode for the RPC service is defined by the threading mode of attached duplex input channel.
        /// </remarks>
        public IThreadDispatcherProvider RpcClientThreading { get; set; }

        /// <summary>
        /// Gets/sets timeout which specifies until when a call to a remote method must return.
        /// </summary>
        /// <remarks>
        /// Default value is TimeSpan.FromMilliseconds(-1) what is the infinite time. 
        /// </remarks>
        public TimeSpan RpcTimeout { get; set; }
    }
}
