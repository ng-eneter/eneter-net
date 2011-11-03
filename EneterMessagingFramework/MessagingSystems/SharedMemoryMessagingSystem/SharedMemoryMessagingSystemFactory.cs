/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if NET4

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages via the shared memory.
    /// </summary>
    /// <remarks>
    /// It creates communication channels for sending and receiving messages via shared memory.<br/>
    /// Communication via the shared memory can transfer messages between applications running on the same machine
    /// and is significantly faster than using named pipes.<br/>
    /// Messaging via the shared memeory is supported only in .Net 4.0 or higher.
    /// </remarks>
    public class SharedMemoryMessagingSystemFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the messaging factory with the default settings.
        /// </summary>
        /// <remarks>
        /// The default constructor creates the factory that will create input and output channels
        /// using the shared memory. <br/>
        /// The maximum message size will be 10Mb.
        /// </remarks>
        public SharedMemoryMessagingSystemFactory()
            : this(10485760)
        {
        }

        /// <summary>
        /// Constructs the messaging system with possibility to specify the maximum message size.
        /// </summary>
        /// <param name="maxMessageSize">maximum message size in bytes</param>
        public SharedMemoryMessagingSystemFactory(int maxMessageSize)
        {
            using (EneterTrace.Entering())
            {
                myMaxMessageSize = maxMessageSize;
            }
        }

        /// <summary>
        /// Creates the output channel sending messages to specified input channel via the shared memory.
        /// </summary>
        /// <remarks>
        /// The output channel can send messages only to the input channel and not to the duplex input channel.
        /// </remarks>
        /// <param name="channelId">Identifies the receiving input channel.
        /// The id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <returns>output channel</returns>
        public IOutputChannel CreateOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SharedMemoryOutputChannel(channelId);
            }
        }

        /// <summary>
        /// Creates the input channel receiving message from the output channel via the shared memory.
        /// </summary>
        /// <remarks>
        /// The input channel can receive messages only from the output channel and not from the duplex output channel.
        /// </remarks>
        /// <param name="channelId">
        /// Identifier of the listening input channel. The id is the name of the memory-mapped file that
        /// will be used to send and receive messages.
        /// </param>
        /// <returns>input channel</returns>
        public IInputChannel CreateInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SharedMemoryInputChannel(channelId, myMaxMessageSize);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages
        /// via shared memory.
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
        /// <param name="channelId">Identifies the receiving duplex input channel.
        /// The id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, null, this);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages
        /// via shared memory.
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
        /// <param name="channelId">Identifies the receiving duplex input channel.
        /// The id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexOutputChannel(channelId, responseReceiverId, this);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages
        /// via the shared memory.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">Identifier of the listening input channel. The id is the name of the memory-mapped file that
        /// will be used to send and receive messages.
        /// </param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new SimpleDuplexInputChannel(channelId, this);
            }
        }


        private int myMaxMessageSize;
    }
}

#endif