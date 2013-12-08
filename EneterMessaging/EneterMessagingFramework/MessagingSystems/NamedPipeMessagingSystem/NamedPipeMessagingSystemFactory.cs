/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO.Pipes;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    /// <summary>
    /// Implementats the messaging system delivering messages via named pipes.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels for sending and receiving messages with using Named Pipes.
    /// The channel id must be a valid URI address. E.g.: net.pipe//127.0.0.1/SomeName/ . <br/>
    /// Notice, Silverlight and Windows Phone 7 do not support Named Pipes.
    /// Therefore, this functionality is not available for these platforms.
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
            public NamedPipeConnectorFactory(int timeOut, int numberOfListeningInstances, PipeSecurity security)
            {
                using (EneterTrace.Entering())
                {
                    myTimeout = timeOut;
                    myNumberOfListeningInstances = numberOfListeningInstances;
                    mySecurity = security;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new NamedPipeOutputConnector(inputConnectorAddress, outputConnectorAddress, myTimeout, myNumberOfListeningInstances, mySecurity);
                }
            }

            public IInputConnector CreateInputConnector(string inputConnecterAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new NamedPipeInputConnector(inputConnecterAddress, myTimeout, myNumberOfListeningInstances, mySecurity);
                }
            }

            private int myNumberOfListeningInstances;
            private int myTimeout;
            private PipeSecurity mySecurity;
        }


        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// The default parameters are: 10 serving threads, 10 seconds timeout for the disconnection.
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
        public NamedPipeMessagingSystemFactory(int numberOfInputchannelListeningThreads, int pipeConnectionTimeout, IProtocolFormatter<byte[]> protocolFormatter)
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
                                               IProtocolFormatter<byte[]> protocolFormatter,
                                               PipeSecurity pipeSecurity)
        {
            using (EneterTrace.Entering())
            {
                myConnectorFactory = new NamedPipeConnectorFactory(pipeConnectionTimeout, numberOfPipeInstances, pipeSecurity);
                myProtocolFormatter = protocolFormatter;

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }


        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Named Pipe.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id represents the name of the pipe and must be a valid Uri address e.g. net.pipe//127.0.0.1/SomeName/ </param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                // Create response receiver id.
                // Note: The response receiver id must be created here because if the id is created by SimpleDuplexOutputChannel,
                //       then the id would not have to be a valid Uri address.
                string aResponseReceiverId = channelId.TrimEnd('/');
                aResponseReceiverId += "_" + Guid.NewGuid().ToString() + "/";

                IDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, aResponseReceiverId, aDispatcher, myConnectorFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Named Pipe.
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
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id represents the name of the pipe and must be a valid Uri address e.g. net.pipe//127.0.0.1/SomeName/ </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel. The id cannot be an Uri address. It must be a plain srting.</param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, myConnectorFactory, myProtocolFormatter, false);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using Named Pipe.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">Identifies this duplex input channel. The channel id represents the name of the pipe and must be a valid Uri address e.g. net.pipe//127.0.0.1/SomeName/ </param>
        /// <returns></returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDispatcher aDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = myConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, aDispatcher, anInputConnector, myProtocolFormatter);
            }
        }


        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex input channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Factory that will create dispatchers responsible for routing events from duplex output channel according to
        /// desired threading strategy.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IDispatcherProvider OutputChannelThreading { get; set; }

        private NamedPipeConnectorFactory myConnectorFactory;
        private IProtocolFormatter<byte[]> myProtocolFormatter;
    }
}


#endif