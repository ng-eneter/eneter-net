using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.AuthenticatedConnection
{
    [TestFixture]
    public class Test_AuthenticatedConnection_Tcp : AuthenticatedConnectionBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();


            // Generate random number for the port.
            string aPort = RandomPortGenerator.Generate();

            TcpMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
            //ChannelId = "tcp://127.0.0.1:" + aPort + "/";
            ChannelId = "tcp://[::1]:" + aPort + "/";

            MessagingSystemFactory = new AuthenticatedMessagingFactory(anUnderlyingMessaging, GetHandshakeResponse, Authenticate);

            myHandshakeSerializer = new AesSerializer("Password123");
        }

        private object GetHandshakeResponse(string handshakeMessage)
        {
            object aHandshakeResponse = myHandshakeSerializer.Serialize<string>(handshakeMessage);
            return aHandshakeResponse;
        }

        private bool Authenticate(string responseReceiverId, string handshakeMessage, object handshakeResponse)
        {
            string aHandshakeResponse = myHandshakeSerializer.Deserialize<string>(handshakeResponse);
            return handshakeMessage == aHandshakeResponse;
        }


        private ISerializer myHandshakeSerializer;
    }
}
