/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    public delegate object GetLoginMessage(string channelId, string responseReceiverId);
    public delegate object GetHanshakeMessage(string channelId, string responseReceiverId, object loginMessage);
    public delegate object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage);
    public delegate bool VerifyHandshakeResponseMessage(string channelId, string responseReceiverId, object loginMessage, object handshakeMessage, object handshakeResponse);

    public class AuthenticatedMessagingFactory : IMessagingSystemFactory
    {
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetLoginMessage getLoginMessageCallback,
            GetHandshakeResponseMessage getHandshakeResponseMessageCallback)
            : this(underlyingMessagingSystem, getLoginMessageCallback, getHandshakeResponseMessageCallback, null, null)
        {
        }

        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetHanshakeMessage getHandshakeMessageCallback,
            VerifyHandshakeResponseMessage verifyHandshakeResponseMessageCallback)
            : this(underlyingMessagingSystem, null, null, getHandshakeMessageCallback, verifyHandshakeResponseMessageCallback)
        {
        }

        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetLoginMessage getLoginMessageCallback,
            GetHandshakeResponseMessage getHandshakeResponseMessageCallback,
            GetHanshakeMessage getHandshakeMessageCallback,
            VerifyHandshakeResponseMessage verifyHandshakeResponseMessageCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessagingSystem;
                AuthenticationTimeout = TimeSpan.FromMilliseconds(10000);

                myGetLoginMessageCallback = getLoginMessageCallback;
                myGetHandShakeMessageCallback = getHandshakeMessageCallback;
                myGetHandshakeResponseMessageCallback = getHandshakeResponseMessageCallback;
                myVerifyHandshakeResponseMessageCallback = verifyHandshakeResponseMessageCallback;
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetLoginMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the login message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myGetHandshakeResponseMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the response message for handshake is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }


                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myGetLoginMessageCallback, myGetHandshakeResponseMessageCallback, AuthenticationTimeout);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetLoginMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the login message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myGetHandshakeResponseMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the response message for handshake is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myGetLoginMessageCallback, myGetHandshakeResponseMessageCallback, AuthenticationTimeout);
            }
        }

        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetHandShakeMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex input channel because the callback to get the handshake message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myVerifyHandshakeResponseMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex input channel because the callback to verify the handshake response message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexInputChannel anUnderlyingInputChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new AuthenticatedDuplexInputChannel(anUnderlyingInputChannel, myGetHandShakeMessageCallback, myVerifyHandshakeResponseMessageCallback);
            }
        }


        /// <summary>
        /// Sets or gets the timeout for the authentication.
        /// </summary>
        /// <remarks>
        /// The authentication timeout is used by dulex output channel when opening connection.
        /// If the connection is open but the authentication exceeds defined time the timeout exception is thrown and the connection is not open.
        /// Default value is 10 seconds.
        /// </remarks>
        public TimeSpan AuthenticationTimeout { get; set; }


        private IMessagingSystemFactory myUnderlyingMessaging;

        private GetLoginMessage myGetLoginMessageCallback;
        private GetHanshakeMessage myGetHandShakeMessageCallback;
        private GetHandshakeResponseMessage myGetHandshakeResponseMessageCallback;
        private VerifyHandshakeResponseMessage myVerifyHandshakeResponseMessageCallback;

        private string TracedObject
        {
            get
            {
                return GetType().Name;
            }
        }
    }
}
