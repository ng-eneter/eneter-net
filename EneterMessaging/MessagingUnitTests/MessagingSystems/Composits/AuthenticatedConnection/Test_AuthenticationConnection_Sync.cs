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
                GetHandshakeMessage, VerifyHandshakeResponseMessage)
            {
                AuthenticationTimeout = TimeSpan.FromMilliseconds(2000)
            };

            myHandshakeSerializer = new AesSerializer("Password123");
        }


        public override void AuthenticationTimeout()
        {
            // Not applicable in synchronous messaging.
        }
    }
}
