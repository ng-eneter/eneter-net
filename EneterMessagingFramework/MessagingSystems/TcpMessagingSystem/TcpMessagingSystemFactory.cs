/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

#if !SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
#endif

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages via TCP.
    /// </summary>
    /// <remarks>
    /// It creates the communication channels using TCP for sending and receiving messages.
    /// The channel id must be a valid URI address. E.g.: tcp://127.0.0.1:6080/. <br/>
    /// Notice, Silverlight and Windows Phone 7 do not support TCP listeners.
    /// Therefore, only sending of messages (and receiving response messages) is possible on these platforms.
    /// </remarks>
    public class TcpMessagingSystemFactory : IMessagingSystemFactory
    {
#if SILVERLIGHT
        /// <summary>
        /// Constructs the factory that will create output channels and duplex output channels with default settings.
        /// </summary>
        /// <remarks>
        /// The send message timeout is set to 30 seconds and the response messages will be received in the main Silverlight thread.
        /// </remarks>
        public TcpMessagingSystemFactory()
            : this(30000, true)
        {
        }

        /// <summary>
        /// Constructs the factory that will create output channels and duplex output channels.
        /// </summary>
        /// <remarks>
        /// The response messages are received in the main Silverlight thread.
        /// </remarks>
        /// <param name="sendMessageTimeout">timeout for sending messages</param>
        public TcpMessagingSystemFactory(int sendMessageTimeout)
            : this(sendMessageTimeout, true)
        {
            using (EneterTrace.Entering())
            {
                mySendMessageTimeout = sendMessageTimeout;
            }
        }

        /// <summary>
        /// Constructs the factory that will create output channels and duplex output channels.
        /// </summary>
        /// <param name="sendMessageTimeout">timeout for sending messages</param>
        /// <param name="receiveInSilverlightThread">true - if the response messages shall be received in the main Silverlight thread.</param>
        public TcpMessagingSystemFactory(int sendMessageTimeout, bool receiveInSilverlightThread)
        {
            using (EneterTrace.Entering())
            {
                mySendMessageTimeout = sendMessageTimeout;
                myIsResponseReceivedInSilverlightThread = receiveInSilverlightThread;
            }
        }
#endif


        /// <summary>
        /// Creates the output channel sending messages to the specified input channel by using TCP.
        /// </summary>
        /// <remarks>
        /// The output channel can send messages only to the input channel and not to the duplex input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving input channel. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                return new TcpOutputChannel(channelId, ClientSecurityStreamFactory);
#else
                return new TcpOutputChannel_Silverlight(channelId, mySendMessageTimeout);
#endif
            }
        }

        /// <summary>
        /// Creates the input channel receiving messages from output channel by using TCP.<br/>
        /// The method is not supported in Silverlight and Windows Phone 7.
        /// </summary>
        /// <remarks>
        /// The input channel can receive messages only from the output channel and not from the duplex output channel.
        /// </remarks>
        /// <param name="channelId">The addres, the input channel will listen to. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                return new TcpInputChannel(channelId, ServerSecurityStreamFactory);
#else
                throw new NotSupportedException("The Tcp input channel is not supported in Silverlight.");
#endif
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using TCP.
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
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                return new TcpDuplexOutputChannel(channelId, null, ClientSecurityStreamFactory);
#else
                return new TcpDuplexOutputChannel_Silverlight(channelId, null, mySendMessageTimeout, myIsResponseReceivedInSilverlightThread);
#endif
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using TCP.
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
        /// <param name="channelId">Identifies the receiving duplex input channel. The channel id must be a valid URI address e.g. tcp://127.0.0.1:8090/ </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                return new TcpDuplexOutputChannel(channelId, responseReceiverId, ClientSecurityStreamFactory);
#else
                return new TcpDuplexOutputChannel_Silverlight(channelId, responseReceiverId, mySendMessageTimeout, myIsResponseReceivedInSilverlightThread);
#endif
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using TCP.
        /// The method is not supported in Silverlight and Windows Phone 7.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">Identifies this duplex input channel. The channel id must be a valid Ip address (e.g. 127.0.0.1:8090) the input channel will listen to.</param>
        /// <returns></returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
#if !SILVERLIGHT
                return new TcpDuplexInputChannel(channelId, ServerSecurityStreamFactory);
#else
                throw new NotSupportedException("The Tcp duplex input channel is not supported in Silverlight.");
#endif
            }
        }

#if SILVERLIGHT
        private int mySendMessageTimeout;
        private bool myIsResponseReceivedInSilverlightThread;
#endif

#if !SILVERLIGHT
        /// <summary>
        /// Sets or gets the security stream factory for the server.
        /// If the factory is set, then the input channel and the duplex input channel use it to establish
        /// the secure communication.
        /// </summary>
        public ISecurityFactory ServerSecurityStreamFactory { get; set; }

        /// <summary>
        /// Sets and gets the security stream factory for the client.
        /// If the factory is set, then the output channel and the duplex output channel use it to establish
        /// the secure communication.
        /// </summary>
        public ISecurityFactory ClientSecurityStreamFactory { get; set; }

#endif
    }
}

#endif