
#if SILVERLIGHT

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.MessagingUnitTests.MessagingSystems;

namespace Eneter.MessagingUnitTests.MessagingSystems.SilverlightMessagingSystem
{
    [TestFixture]
    public class Test_SilverlightMessagingSytem : MessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ILocalSenderReceiverFactory aLocalSenderReceiverFactory = new TestingLocalSenderReceiverFactory();
            MessagingSystemFactory = new SilverlightMessagingSystemFactory(aLocalSenderReceiverFactory);
        }
    }
}

#endif //SILVERLIGHT