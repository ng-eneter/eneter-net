using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    public delegate bool AuthenticateCallback(string responseReceiverId, string handshakeMessage, object handshakeResponse);
    public delegate object GetHandshakeResponseCallback(string handshakeMessage);

    public class AuthenticatedMessagingFactory : IMessagingSystemFactory
    {
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            AuthenticateCallback authenticateCallback)
            : this(underlyingMessagingSystem, null, authenticateCallback)
        {
        }

        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetHandshakeResponseCallback handshakeResponseCallback)
            : this(underlyingMessagingSystem, handshakeResponseCallback, null)
        {
        }

        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetHandshakeResponseCallback handshakeResponseCallback,
            AuthenticateCallback authenticateCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessagingSystem;
                myHandshakeResponseCallback = handshakeResponseCallback;
                myAuthenticateCallback = authenticateCallback;
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myHandshakeResponseCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback for handshake response is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myHandshakeResponseCallback);
            }
        }

        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (myHandshakeResponseCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback for handshake response is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myHandshakeResponseCallback);
            }
        }

        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myAuthenticateCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex input channel because the callback for authentication is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexInputChannel anUnderlyingInputChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new AuthenticatedDuplexInputChannel(anUnderlyingInputChannel, myAuthenticateCallback);
            }
        }


        private IMessagingSystemFactory myUnderlyingMessaging;
        private GetHandshakeResponseCallback myHandshakeResponseCallback;
        private AuthenticateCallback myAuthenticateCallback;

        private string TracedObject
        {
            get
            {
                return GetType().Name;
            }
        }
    }
}
