#if !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using System.Threading;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.AuthenticatedConnection
{
    public abstract class AuthenticatedConnectionBaseTester : BaseTester
    {
        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public virtual void AuthenticationTimeout()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            try
            {
                myAuthenticationSleep = TimeSpan.FromMilliseconds(3000);

                anInputChannel.StartListening();

                // Client opens the connection.
                anOutputChannel.OpenConnection();
            }
            finally
            {
                myAuthenticationSleep = TimeSpan.FromMilliseconds(0);

                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
            }
        }


        protected object GetLoginMessage(string channelId, string responseReceiverId)
        {
            return "MyLoginName";
        }

        protected object GetHandshakeMessage(string channelId, string responseReceiverId, object loginMessage)
        {
            // Sleep in case a timeout is needed.
            Thread.Sleep(myAuthenticationSleep);

            if ((string)loginMessage == "MyLoginName")
            {
                return "MyHandshake";
            }

            return null;
        }

        protected object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage)
        {
            object aHandshakeResponse = myHandshakeSerializer.Serialize<string>((string)handshakeMessage);
            return aHandshakeResponse;
        }

        protected bool VerifyHandshakeResponseMessage(string channelId, string responseReceiverId, object loginMassage, object handshakeMessage, object handshakeResponse)
        {
            string aHandshakeResponse = myHandshakeSerializer.Deserialize<string>(handshakeResponse);
            return (string)handshakeMessage == aHandshakeResponse;
        }


        protected ISerializer myHandshakeSerializer;
        protected TimeSpan myAuthenticationSleep = TimeSpan.FromMilliseconds(0);
    }
}

#endif