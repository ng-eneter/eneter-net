/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !XAMARIN && !NETSTANDARD20

using System;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via named pipes.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels for sending and receiving messages using Named Pipes.
    /// The channel id must be a valid URI address. E.g.: net.pipe//127.0.0.1/SomeName/ .
    /// <br/><br/>
    /// The input channel creates the pipe for the reading and waits for connections. To handle more connections
    /// in parallel there are more threads serving them (by default 10 threads). Every such thread waits
    /// for messages and puts them to the message queue. The message queue is connected to one working thread
    /// that removes messages one by one and notifies subscribers of the input channel.
    /// Therefore the subscribers are notified always from the same working thread.
    /// </remarks>
    public class NamedPipeMessagingSystemFactory : IMessagingSystemFactory
    {
        private class NamedPipeConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public NamedPipeConnectorFactory(IProtocolFormatter protocolFormatter, int connectionTimeout, int numberOfListeningInstances, PipeSecurity security)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myConnectionTimeout = connectionTimeout;
                    myNumberOfListeningInstances = numberOfListeningInstances;
                    mySecurity = security;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new NamedPipeOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myConnectionTimeout, mySecurity);
                }
            }

            public IInputConnector CreateInputConnector(string inputConnecterAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new NamedPipeInputConnector(inputConnecterAddress, myProtocolFormatter, myConnectionTimeout, myNumberOfListeningInstances, mySecurity);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private int myNumberOfListeningInstances;
            private int myConnectionTimeout;
            private PipeSecurity mySecurity;
        }


        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// The default parameters are: 10 serving threads, 10 seconds for connection timeout.
        /// </remarks>
        public NamedPipeMessagingSystemFactory()
            : this(10, 10000, new EneterProtocolFormatter(), null)
        {
        }

        /// <summary>
        /// Constructs the factory specifying the number of processing threads in the input channel and the timeout
        /// for the output channel.
        /// </summary>
        /// <param name="numberOfInputchannelListeningThreads">
        /// Number of threads that will listen in parallel in input channels created by the factory.
        /// The maximum number is 254. Many threads can increase the number of processing connections at the same time
        /// but it consumes a lot of resources.
        /// </param>
        /// <param name="pipeConnectionTimeout">
        /// The maximum time in miliseconds, the output channel waits to connect the pipe and sends the message.
        /// </param>
        public NamedPipeMessagingSystemFactory(int numberOfInputchannelListeningThreads, int pipeConnectionTimeout)
            : this(numberOfInputchannelListeningThreads, pipeConnectionTimeout, new EneterProtocolFormatter(), null)
        {
        }

        /// <summary>
        /// Constructs the factory specifying the number of processing threads in the input channel and the timeout
        /// for the output channel. It also allows to specify the security settings for the pipe.
        /// </summary>
        /// <remarks>
        /// Security settings can be needed, if communicating processes run under different integrity levels.
        /// E.g. If the service runs under administrator account and the client under some user account,
        /// then the communication will not work until the pipe security is not set.
        /// (Client will get access denied exception.)
        /// <example>
        /// The following example shows how to set the pipe security on the service running under administrator
        /// account to be accessible from client processes.
        /// <code>
        /// PipeSecurity aPipeSecurity = new PipeSecurity();
        /// 
        /// // Set to low integrity level.
        /// aPipeSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
        /// 
        /// SecurityIdentifier aSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        /// ipeAccessRule aPipeAccessRule = new PipeAccessRule(aSid, PipeAccessRights.ReadWrite, AccessControlType.Allow);
        /// aPipeSecurity.AddAccessRule(aPipeAccessRule);
        /// 
        /// // Create the messaging communicating via Named Pipes.
        /// IMessagingSystemFactory aMessagingSystem = new NamedPipeMessagingSystemFactory(10, 10000, aPipeSecurity);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="numberOfInputchannelListeningThreads">
        /// Number of threads that will listen in parallel in input channels created by the factory.
        /// The maximum number is 254. Many threads can increase the number of processing connections at the same time
        /// but it consumes a lot of resources.
        /// </param>
        /// <param name="pipeConnectionTimeout">
        /// The maximum time in miliseconds, the output channel waits to connect the pipe and sends the message.</param>
        /// <param name="protocolFormatter">formatter of low-level messages between channels</param>
        public NamedPipeMessagingSystemFactory(int numberOfInputchannelListeningThreads, int pipeConnectionTimeout, IProtocolFormatter protocolFormatter)
            : this(numberOfInputchannelListeningThreads, pipeConnectionTimeout, protocolFormatter, null)
        {
        }

        /// <summary>
        /// Constructs the factory specifying the number of processing threads in the input channel and the timeout
        /// for the output channel. It also allows to specify the security settings for the pipe.
        /// </summary>
        /// <remarks>
        /// Security settings can be needed, if communicating processes run under different integrity levels.
        /// E.g. If the service runs under administrator account and the client under some user account,
        /// then the communication will not work until the pipe security is not set.
        /// (Client will get access denied exception.)
        /// <example>
        /// The following example shows how to set the pipe security on the service running under administrator
        /// account to be accessible from client processes.
        /// <code>
        /// PipeSecurity aPipeSecurity = new PipeSecurity();
        /// 
        /// // Set to low integrity level.
        /// aPipeSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
        /// 
        /// SecurityIdentifier aSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        /// ipeAccessRule aPipeAccessRule = new PipeAccessRule(aSid, PipeAccessRights.ReadWrite, AccessControlType.Allow);
        /// aPipeSecurity.AddAccessRule(aPipeAccessRule);
        /// 
        /// // Create the messaging communicating via Named Pipes.
        /// IMessagingSystemFactory aMessagingSystem = new NamedPipeMessagingSystemFactory(10, 10000, aPipeSecurity);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="numberOfPipeInstances">
        /// Number of clients that can be connected at the same time.
        /// The maximum number is 254. Many threads can increase the number of processing connections at the same time
        /// but it consumes a lot of resources.
        /// </param>
        /// <param name="pipeConnectionTimeout">
        /// The maximum time in miliseconds, the output channel waits to connect the pipe and sends the message.</param>
        /// <param name="protocolFormatter">formatter of low-level messages between channels</param>
        /// <param name="pipeSecurity">
        /// Pipe security.
        /// </param>
        public NamedPipeMessagingSystemFactory(int numberOfPipeInstances, int pipeConnectionTimeout,
                                               IProtocolFormatter protocolFormatter,
                                               PipeSecurity pipeSecurity)
        {
            using (EneterTrace.Entering())
            {
                myConnectorFactory = new NamedPipeConnectorFactory(protocolFormatter, pipeConnectionTimeout, numberOfPipeInstances, pipeSecurity);

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }


        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using Named Pipes.
        /// </summary>
        /// <param name="channelId">Identifies the input channel which shall be connected. The channel id must be a valid URI address e.g. net.pipe://127.0.0.1/SomeName/ </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                // Create response receiver id.
                // Note: The response receiver id must be created here because if the id is created by SimpleDuplexOutputChannel,
                //       then the id would not have to be a valid Uri address.
                string aResponseReceiverId = channelId.TrimEnd('/');
                aResponseReceiverId += "_" + Guid.NewGuid().ToString() + "/";

                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, aResponseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        ///  Creates duplex output channel which can send and receive messages from the duplex input channel using Named Pipes.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method allows to specified a desired response receiver id. Please notice, the response receiver
        /// id is supposed to be unique.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the input channel which shall be connected. The channel id must be a valid URI address e.g. net.pipe://127.0.0.1/SomeName/ </param>
        /// <param name="responseReceiverId">Unique identifier of the output channel. If null then the id is generated automatically.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        /// Creates the duplex input channel which can receive and send messages to the duplex output channel using Named Pipe.
        /// </summary>
        /// <param name="channelId">Named pipe address which shall be used for the listening. E.g. net.pipe//127.0.0.1/SomeName/ </param>
        /// <returns></returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, aDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }


        /// <summary>
        /// Sets or gets the threading mode for input channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for output channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        private NamedPipeConnectorFactory myConnectorFactory;
        private IThreadDispatcher myDispatcherAfterMessageDecoded = new NoDispatching().GetDispatcher();
    }
}


#endif