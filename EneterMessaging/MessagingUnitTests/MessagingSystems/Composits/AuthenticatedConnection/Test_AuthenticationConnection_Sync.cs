using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.AuthenticatedConnection
{
    [TestFixture]
    public class Test_AuthenticationConnection_Sync : AuthenticatedConnectionBaseTester
    {

        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            SynchronousMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            ChannelId = "MyChannel1";

            MessagingSystemFactory = new AuthenticatedMessagingFactory(anUnderlyingMessaging,
                GetLoginMessage,
                GetHandshakeResponseMessage,
                GetHandshakeMessage, VerifyHandshakeResponseMessage);

            myHandshakeSerializer = new AesSerializer("Password123");
        }

        private object GetLoginMessage(string channelId, string responseReceiverId)
        {
            return "MyLoginName";
        }

        private object GetHandshakeMessage(string channelId, string responseReceiverId, object loginMessage)
        {
            if ((string)loginMessage == "MyLoginName")
            {
                return "MyHandshake";
            }

            return null;
        }

        private object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage)
        {
            object aHandshakeResponse = myHandshakeSerializer.Serialize<string>((string)handshakeMessage);
            return aHandshakeResponse;
        }

        private bool VerifyHandshakeResponseMessage(string channelId, string responseReceiverId, object loginMassage, object handshakeMessage, object handshakeResponse)
        {
            string aHandshakeResponse = myHandshakeSerializer.Deserialize<string>(handshakeResponse);
            return (string)handshakeMessage == aHandshakeResponse;
        }


        private ISerializer myHandshakeSerializer;
    }
}
