#if !COMPACT_FRAMEWORK

using System;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;

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

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public virtual void ConnectionNotGranted()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            try
            {
                myConnectionNotGranted = true;

                anInputChannel.StartListening();

                // Client opens the connection.
                anOutputChannel.OpenConnection();
            }
            finally
            {
                myConnectionNotGranted = false;

                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void AuthenticationCancelledByClient()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            try
            {
                myClientCancelAuthentication = true;

                anInputChannel.StartListening();

                Exception anException = null;
                try
                {
                    // Client opens the connection.
                    anOutputChannel.OpenConnection();
                }
                catch (Exception err)
                {
                    anException = err;
                }

                Assert.IsInstanceOf<InvalidOperationException>(anException);

                // Check that the AuthenticationCancelled calleback was called.
                Assert.IsTrue(myAuthenticationCancelled);
            }
            finally
            {
                myClientCancelAuthentication = false;
                myAuthenticationCancelled = false;

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

            if (myConnectionNotGranted)
            {
                return null;
            }

            if ((string)loginMessage == "MyLoginName")
            {
                return "MyHandshake";
            }

            return null;
        }

        protected object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage)
        {
            if (myClientCancelAuthentication)
            {
                return null;
            }

            object aHandshakeResponse = myHandshakeSerializer.Serialize<string>((string)handshakeMessage);
            return aHandshakeResponse;
        }

        protected bool VerifyHandshakeResponseMessage(string channelId, string responseReceiverId, object loginMassage, object handshakeMessage, object handshakeResponse)
        {
            string aHandshakeResponse = myHandshakeSerializer.Deserialize<string>(handshakeResponse);
            return (string)handshakeMessage == aHandshakeResponse;
        }

        protected void HandleAuthenticationCancelled(string channelId, string responseReceiverId, object loginMassage)
        {
            myAuthenticationCancelled = true;
        }


        protected ISerializer myHandshakeSerializer;
        protected TimeSpan myAuthenticationSleep = TimeSpan.FromMilliseconds(0);
        protected bool myClientCancelAuthentication;
        protected bool myAuthenticationCancelled;
        protected bool myConnectionNotGranted;
    }
}

#endif