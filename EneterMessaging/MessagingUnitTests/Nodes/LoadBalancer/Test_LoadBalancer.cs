
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ThreadMessagingSystem;
using Eneter.Messaging.Nodes.LoadBalancer;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.Nodes.LoadBalancer
{
    [TestFixture]
    public class Test_LoadBalancer
    {
        public class Interval
        {
            public Interval()
            {
            }

            public Interval(double from, double to)
            {
                From = from;
                To = to;
            }

            public double From;
            public double To;
        }

        private class CalculatorService : IDisposable
        {
            public CalculatorService(string address, IMessagingSystemFactory messaging)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();

                    myRequestReceiver = aReceiverFactory.CreateDuplexTypedMessageReceiver<double, Interval>();
                    myRequestReceiver.MessageReceived += OnMessageReceived;

                    IDuplexInputChannel anInputChannel = messaging.CreateDuplexInputChannel(address);
                    myRequestReceiver.AttachDuplexInputChannel(anInputChannel);
                }
            }

            public void Dispose()
            {
                using (EneterTrace.Entering())
                {
                    myRequestReceiver.DetachDuplexInputChannel();
                }
            }

            private void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<Interval> e)
            {
                using (EneterTrace.Entering())
                {
                    EneterTrace.Debug(string.Format("Address: {0} From: {1} To: {2}",
                        myRequestReceiver.AttachedDuplexInputChannel.ChannelId,
                        e.RequestMessage.From, e.RequestMessage.To));

                    double aResult = 0.0;
                    double aDx = 0.000000001;
                    for (double x = e.RequestMessage.From; x < e.RequestMessage.To; x += aDx)
                    {
                        aResult += 2 * Math.Sqrt(1 - x * x) * aDx;
                    }

                    myRequestReceiver.SendResponseMessage(e.ResponseReceiverId, aResult);
                }
            }

            private IDuplexTypedMessageReceiver<double, Interval> myRequestReceiver;

        }


        [Test]
        public void CalculatePi()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            IMessagingSystemFactory aThreadMessaging = new ThreadMessagingSystemFactory();

            List<CalculatorService> aServices = new List<CalculatorService>();

            ILoadBalancer aDistributor = null;
            IDuplexTypedMessageSender<double, Interval> aSender = null;

            // Create 50 calculating services.
            try
            {
                for (int i = 0; i < 50; ++i)
                {
                    aServices.Add(new CalculatorService("a" + i.ToString(), aThreadMessaging));
                }

                // Create Distributor
                ILoadBalancerFactory aDistributorFactory = new RoundRobinBalancerFactory(aThreadMessaging);
                aDistributor = aDistributorFactory.CreateLoadBalancer();

                // Attach available services to the distributor.
                for (int i = 0; i < aServices.Count; ++i)
                {
                    aDistributor.AddDuplexOutputChannel("a" + i.ToString());
                }

                // Attach input channel to the distributor.
                IDuplexInputChannel anInputChannel = aThreadMessaging.CreateDuplexInputChannel("DistributorAddress");
                aDistributor.AttachDuplexInputChannel(anInputChannel);


                // Create client that needs to calculate PI.
                IDuplexTypedMessagesFactory aTypedMessagesFactory = new DuplexTypedMessagesFactory();
                aSender = aTypedMessagesFactory.CreateDuplexTypedMessageSender<double, Interval>();

                AutoResetEvent aCalculationCompletedEvent = new AutoResetEvent(false);
                int aCount = 0;
                double aPi = 0.0;
                aSender.ResponseReceived += (x, y) =>
                    {
                        ++aCount;
                        EneterTrace.Debug("Completed interval: " + aCount.ToString());

                        aPi += y.ResponseMessage;

                        if (aCount == 400)
                        {
                            aCalculationCompletedEvent.Set();
                        }
                    };

                IDuplexOutputChannel anOutputChannel = aThreadMessaging.CreateDuplexOutputChannel("DistributorAddress");
                aSender.AttachDuplexOutputChannel(anOutputChannel);

                // Sender sends several parallel requests to calculate specified intervals.
                // 2 / 0.005 = 400 intervals.
                for (double i = -1.0; i <= 1.0; i += 0.005)
                {
                    Interval anInterval = new Interval(i, i + 0.005);
                    aSender.SendRequestMessage(anInterval);
                }

                // Wait until all requests are calculated.
                EneterTrace.Debug("Test waits until completion.");
                aCalculationCompletedEvent.WaitOne();

                EneterTrace.Info("Calculated PI = " + aPi.ToString());
            }
            catch (Exception err)
            {
                EneterTrace.Error("Test failed", err);
                throw;
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aDistributor.DetachDuplexInputChannel();
                aServices.ForEach(x => x.Dispose());
            }
        }


        [Test]
        public void LoadBalancerConnectionLogic()
        {
            IMessagingSystemFactory aMessaging = new SynchronousMessagingSystemFactory();

            string aReceiverChannelId = "";
            EventHandler<DuplexChannelMessageEventArgs> aRequestReceived = (x, y) =>
                {
                    // Echo the message.
                    IDuplexInputChannel anInputChannel = (IDuplexInputChannel)x;
                    aReceiverChannelId = anInputChannel.ChannelId;
                    anInputChannel.SendResponseMessage(y.ResponseReceiverId, y.Message);
                };

            string aResponseReceiverId = "";
            ResponseReceiverEventArgs aDisconnectedLoadBalancerConnection = null;
            EventHandler<ResponseReceiverEventArgs> aDisconnectedFromReceiver = (x, y) =>
                {
                    aResponseReceiverId = ((IDuplexInputChannel)x).ChannelId;
                    aDisconnectedLoadBalancerConnection = y;
                };

            // Receivers.
            IDuplexInputChannel[] aReceivers = new IDuplexInputChannel[3];
            for (int i = 0; i < aReceivers.Length; ++i)
            {
                aReceivers[i] = aMessaging.CreateDuplexInputChannel((i + 1).ToString());
                aReceivers[i].MessageReceived += aRequestReceived;
                aReceivers[i].ResponseReceiverDisconnected += aDisconnectedFromReceiver;
            }

            // Clients.
            IDuplexOutputChannel aClient1 = aMessaging.CreateDuplexOutputChannel("LB");
            string aReceivedResponse1 = "";
            aClient1.ResponseMessageReceived += (x, y) =>
                {
                    aReceivedResponse1 = (string)y.Message;
                };

            IDuplexOutputChannel aClient2 = aMessaging.CreateDuplexOutputChannel("LB");
            string aReceivedResponse2 = "";
            aClient2.ResponseMessageReceived += (x, y) =>
                {
                    aReceivedResponse2 = (string)y.Message;
                };


            // Load Balancer
            ILoadBalancerFactory aLoadBalancerFactory = new RoundRobinBalancerFactory(aMessaging);
            ILoadBalancer aLoadBalancer = aLoadBalancerFactory.CreateLoadBalancer();

            AutoResetEvent aReceiverRemovedEvent = new AutoResetEvent(false);
            string aRemovedReceiverId = "";
            aLoadBalancer.RequestReceiverRemoved += (x, y) =>
                {
                    aRemovedReceiverId = y.ChannelId;
                    aReceiverRemovedEvent.Set();
                };



            try
            {
                // Receivers start listening.
                foreach (IDuplexInputChannel aReceiver in aReceivers)
                {
                    aReceiver.StartListening();
                }

                // Load Balancer starts listening.
                for (int i = 0; i < aReceivers.Length; ++i)
                {
                    aLoadBalancer.AddDuplexOutputChannel((i + 1).ToString());
                }
                aLoadBalancer.AttachDuplexInputChannel(aMessaging.CreateDuplexInputChannel("LB"));

                // Clients connect the load balancer.
                aClient1.OpenConnection();
                aClient2.OpenConnection();

                // It shall use the next (second) receiver.
                aClient1.SendMessage("Hello.");
                Assert.AreEqual("Hello.", aReceivedResponse1);
                Assert.AreEqual("", aReceivedResponse2);
                Assert.AreEqual("2", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                // It shall use the next (third) receiver.
                aClient2.SendMessage("Hello.");
                Assert.AreEqual("", aReceivedResponse1);
                Assert.AreEqual("Hello.", aReceivedResponse2);
                Assert.AreEqual("3", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                // It is at the end of the pool so it shall starts from the beginning.
                aClient2.SendMessage("Hello.");
                Assert.AreEqual("", aReceivedResponse1);
                Assert.AreEqual("Hello.", aReceivedResponse2);
                Assert.AreEqual("1", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                // Let's remove the second receiver.
                aLoadBalancer.RemoveDuplexOutputChannel("2");
                Assert.IsNotNull(aDisconnectedLoadBalancerConnection);
                Assert.AreEqual("2", aResponseReceiverId);

                // The 2nd is removed so it shall use the 3rd one.
                aClient2.SendMessage("Hello.");
                Assert.AreEqual("", aReceivedResponse1);
                Assert.AreEqual("Hello.", aReceivedResponse2);
                Assert.AreEqual("3", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                // 1st receiver stops to listen.
                aReceivers[0].StopListening();

                // It should start from the beginnng but 1st is not listening and 2nd was removed.
                // So it shall use the 3rd one. 
                aClient1.SendMessage("Hello.");
                Assert.AreEqual("Hello.", aReceivedResponse1);
                Assert.AreEqual("", aReceivedResponse2);
                Assert.AreEqual("3", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                aReceiverRemovedEvent.WaitOne();
                Assert.AreEqual("1", aRemovedReceiverId);


                aReceivers[2].StopListening();

                aClient2.SendMessage("Hello.");
                Assert.AreEqual("", aReceivedResponse1);
                Assert.AreEqual("", aReceivedResponse2);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";

                aReceiverRemovedEvent.WaitOne();
                Assert.AreEqual("3", aRemovedReceiverId);

                aReceivers[0].StartListening();
                aReceivers[2].StartListening();

                for (int i = 0; i < aReceivers.Length; ++i)
                {
                    aLoadBalancer.AddDuplexOutputChannel((i + 1).ToString());
                }

                aClient2.SendMessage("Hello.");
                Assert.AreEqual("", aReceivedResponse1);
                Assert.AreEqual("Hello.", aReceivedResponse2);
                Assert.AreEqual("2", aReceiverChannelId);
                aReceivedResponse1 = "";
                aReceivedResponse2 = "";
            }
            finally
            {
                aClient1.CloseConnection();
                aClient2.CloseConnection();
                aLoadBalancer.DetachDuplexInputChannel();
                foreach (IDuplexInputChannel aReceiver in aReceivers)
                {
                    aReceiver.StopListening();
                }
            }

        }

    }
}


#endif