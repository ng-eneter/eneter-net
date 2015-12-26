using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.Rpc;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using System.Diagnostics;
using Eneter.Messaging.Diagnostic;
using System.Threading;


namespace Eneter.MessagingUnitTests.EndPoints.Rpc
{
    public abstract class RpcBaseTester
    {
        [Serializable]
        public class OpenArgs : EventArgs
        {
            public string Name { get; set; }
            public string InstanceId { get; set; }
        }

        public interface IHello
        {
            event EventHandler<OpenArgs> Open;
            event EventHandler Close;

            int Sum(int a, int b);
            string CreateString(string src);
            string GetInstanceId();
            void HelloVoid();

            void Fail();
            void Timeout();
        }

        public class HelloService : IHello
        {
            public event EventHandler<OpenArgs> Open;
            public event EventHandler Close;

            public HelloService()
            {
                myInstanceId = Guid.NewGuid().ToString();
            }

            public int Sum(int a, int b)
            {
                return a + b;
            }

            public string CreateString(string src)
            {
                return src;
            }

            public string GetInstanceId()
            {
                if (Open != null)
                {
                    OpenArgs anArgs = new OpenArgs()
                    {
                        InstanceId = myInstanceId,
                        Name = "Hello"
                    };
                    Open(this, anArgs);
                }

                return myInstanceId;
            }

            public void HelloVoid()
            {
                // this is just a void method - to test null input and null return.
            }

            public void Fail()
            {
                throw new InvalidOperationException("My testing exception.");
            }

            public void Timeout()
            {
                Thread.Sleep(2000);
            }

            public void RaiseOpen(OpenArgs openArgs)
            {
                if (Open != null)
                {
                    Open(this, openArgs);
                }
            }

            public void RaiseClose()
            {
                if (Close != null)
                {
                    Close(this, new EventArgs());
                }
            }


            private string myInstanceId;
        }


        private class EmptySerializer : ISerializer
        {
            public object Serialize<_T>(_T dataToSerialize)
            {
                return dataToSerialize;
            }

            public _T Deserialize<_T>(object serializedData)
            {
                return (_T)serializedData;
            }
        }

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcCall()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                IHello aServiceProxy = anRpcClient.Proxy;
                int k = aServiceProxy.Sum(1, 2);

                Assert.AreEqual(3, k);

                anRpcClient.Proxy.HelloVoid();
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcCall_NullArgument()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                IHello aServiceProxy = anRpcClient.Proxy;
                string k = aServiceProxy.CreateString(null);

                Assert.AreEqual(null, k);
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

        [Test]
        public void DynamicRpcCall()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                int k = (int) anRpcClient.CallRemoteMethod("Sum", 1, 2);

                Assert.AreEqual(3, k);
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }

#if !COMPACT_FRAMEWORK
        [Test]
        [ExpectedException(typeof(RpcException))]
        public void RpcCallError()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                IHello aServiceProxy = anRpcClient.Proxy;
                aServiceProxy.Fail();
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK

        // Note: This test is not applicable for the synchronous messaging
        //       because synchronous messaging is a sequence within one thread and so the remote call
        //       does not wait.
        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public virtual void RpcTimeout()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            anRpcFactory.RpcTimeout = TimeSpan.FromSeconds(1);

            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                IHello aServiceProxy = anRpcClient.Proxy;
                aServiceProxy.Timeout();
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcNonGenericEvent()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                IHello aServiceProxy = anRpcClient.Proxy;

                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                Action<object, EventArgs> anEventHandler = (x, y) =>
                    {
                        anEventReceived.Set();
                    };

                // Subscribe.
                aServiceProxy.Close += anEventHandler.Invoke;

                // Raise the event in the service.
                aService.RaiseClose();

                Assert.IsTrue(anEventReceived.WaitOne());

                // Unsubscribe.
                aServiceProxy.Close -= anEventHandler.Invoke;

                // Try to raise again.
                aService.RaiseClose();

                Assert.IsFalse(anEventReceived.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

        [Test]
        public void DynamicRpcNonGenericEvent()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                EventHandler<EventArgs> anEventHandler = (x, y) =>
                {
                    anEventReceived.Set();
                };

                // Subscribe.
                anRpcClient.SubscribeRemoteEvent<EventArgs>("Close", anEventHandler);

                // Raise the event in the service.
                aService.RaiseClose();

                Assert.IsTrue(anEventReceived.WaitOne());

                // Unsubscribe.
                anRpcClient.UnsubscribeRemoteEvent("Close", anEventHandler);

                // Try to raise again.
                aService.RaiseClose();

                Assert.IsFalse(anEventReceived.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }

        
#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcGenericEvent()
        {

            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                IHello aServiceProxy = anRpcClient.Proxy;

                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                Action<object, OpenArgs> anEventHandler = (x, y) =>
                    {
                        anEventReceived.Set();
                    };

                // Subscribe.
                aServiceProxy.Open += anEventHandler.Invoke;

                // Raise the event in the service.
                OpenArgs anOpenArgs = new OpenArgs()
                {
                    Name = "Hello"
                };
                aService.RaiseOpen(anOpenArgs);

                Assert.IsTrue(anEventReceived.WaitOne());

                // Unsubscribe.
                aServiceProxy.Open -= anEventHandler.Invoke;

                // Try to raise again.
                aService.RaiseOpen(anOpenArgs);

                Assert.IsFalse(anEventReceived.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif
        
        [Test]
        public void DynamicRpcGenericEvent()
        {

            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                string aReceivedEvent = "";
                EventHandler<OpenArgs> anEventHandler = (x, y) =>
                    {
                        aReceivedEvent = y.Name;
                        anEventReceived.Set();
                    };

                // Subscribe.
                anRpcClient.SubscribeRemoteEvent("Open", anEventHandler);

                // Raise the event in the service.
                OpenArgs anOpenArgs = new OpenArgs()
                {
                    Name = "Hello"
                };
                aService.RaiseOpen(anOpenArgs);

                Assert.IsTrue(anEventReceived.WaitOne());
                Assert.AreEqual("Hello", aReceivedEvent);

                // Unsubscribe.
                anRpcClient.UnsubscribeRemoteEvent("Open", anEventHandler);

                // Try to raise again.
                aService.RaiseOpen(anOpenArgs);

                Assert.IsFalse(anEventReceived.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcGenericEvent_SerializerPerClient()
        {
            string aFirstClientId = null;

            RpcFactory anRpcClientFactory1 = new RpcFactory(new XmlStringSerializer());
            RpcFactory anRpcClientFactory2 = new RpcFactory(new BinarySerializer());
            RpcFactory anRpcServiceFactory = new RpcFactory()
            {
                SerializerProvider = x => (x == aFirstClientId) ? (ISerializer)new XmlStringSerializer() : (ISerializer)new BinarySerializer()
            };
            
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcServiceFactory.CreateSingleInstanceService<IHello>(aService);
            anRpcService.ResponseReceiverConnected += (x, y) => aFirstClientId = aFirstClientId ?? y.ResponseReceiverId;

            IRpcClient<IHello> anRpcClient1 = anRpcClientFactory1.CreateClient<IHello>();
            IRpcClient<IHello> anRpcClient2 = anRpcClientFactory2.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient1.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId, "Client1"));
                anRpcClient2.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId, "Client2"));

                AutoResetEvent anEvent1Received = new AutoResetEvent(false);
                Action<object, OpenArgs> anEventHandler1 = (x, y) =>
                {
                    anEvent1Received.Set();
                };

                AutoResetEvent anEvent2Received = new AutoResetEvent(false);
                Action<object, OpenArgs> anEventHandler2 = (x, y) =>
                {
                    anEvent2Received.Set();
                };

                // Subscribe.
                anRpcClient1.Proxy.Open += anEventHandler1.Invoke;
                anRpcClient2.Proxy.Open += anEventHandler2.Invoke;

                // Raise the event in the service.
                OpenArgs anOpenArgs = new OpenArgs()
                {
                    Name = "Hello"
                };
                aService.RaiseOpen(anOpenArgs);

                anEvent1Received.WaitIfNotDebugging(1000);
                anEvent2Received.WaitIfNotDebugging(1000);

                // Unsubscribe.
                anRpcClient1.Proxy.Open -= anEventHandler1.Invoke;
                anRpcClient2.Proxy.Open -= anEventHandler2.Invoke;

                // Try to raise again.
                aService.RaiseOpen(anOpenArgs);

                Assert.IsFalse(anEvent1Received.WaitOne(1000));
                Assert.IsFalse(anEvent2Received.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient1.IsDuplexOutputChannelAttached)
                {
                    anRpcClient1.DetachDuplexOutputChannel();
                }

                if (anRpcClient2.IsDuplexOutputChannelAttached)
                {
                    anRpcClient2.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void SubscribeBeforeAttachOutputChannel()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                IHello aServiceProxy = anRpcClient.Proxy;

                AutoResetEvent aClientConnected = new AutoResetEvent(false);
                anRpcClient.ConnectionOpened += (x, y) =>
                    {
                        aClientConnected.Set();
                    };

                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                Action<object, EventArgs> anEventHandler = (x, y) =>
                    {
                        anEventReceived.Set();
                    };

                // Subscribe before the connection is open.
                aServiceProxy.Close += anEventHandler.Invoke;

                // Open the connection.
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                // Wait until the client is connected.
                aClientConnected.WaitOne();

                // Raise the event in the service.
                aService.RaiseClose();

                Assert.IsTrue(anEventReceived.WaitOne());

                // Unsubscribe.
                aServiceProxy.Close -= anEventHandler.Invoke;

                // Try to raise again.
                aService.RaiseClose();

                Assert.IsFalse(anEventReceived.WaitOne(1000));
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcNonGenericEvent_10000()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                IHello aServiceProxy = anRpcClient.Proxy;

                int aCounter = 0;
                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                Action<object, EventArgs> anEventHandler = (x, y) =>
                    {
                        ++aCounter;
                        if (aCounter == 10000)
                        {
                            anEventReceived.Set();
                        }
                    };

                // Subscribe.
                aServiceProxy.Close += anEventHandler.Invoke;

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                // Raise the event in the service.
                for (int i = 0; i < 10000; ++i)
                {
                    aService.RaiseClose();
                }

                Assert.IsTrue(anEventReceived.WaitOne());

                aStopWatch.Stop();
                Console.WriteLine("Remote event. Elapsed time = " + aStopWatch.Elapsed);

                // Unsubscribe.
                aServiceProxy.Close -= anEventHandler.Invoke;
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

        [Test]
        public void DynamicRpcNonGenericEvent_10000()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            HelloService aService = new HelloService();
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                int aCounter = 0;
                AutoResetEvent anEventReceived = new AutoResetEvent(false);
                EventHandler<EventArgs> anEventHandler = (x, y) =>
                    {
                        ++aCounter;
                        if (aCounter == 10000)
                        {
                            anEventReceived.Set();
                        }
                    };

                // Subscribe.
                anRpcClient.SubscribeRemoteEvent("Close", anEventHandler);

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                // Raise the event in the service.
                for (int i = 0; i < 10000; ++i)
                {
                    aService.RaiseClose();
                }

                aStopWatch.Stop();
                Console.WriteLine("Remote event. Elapsed time = " + aStopWatch.Elapsed);

                Assert.IsTrue(anEventReceived.WaitOne());

                // Unsubscribe.
                anRpcClient.UnsubscribeRemoteEvent("Close", anEventHandler);
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }

#if !COMPACT_FRAMEWORK
        [Test]
        public void RpcCall_10000()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());
            IRpcClient<IHello> anRpcClient = anRpcFactory.CreateClient<IHello>();

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));

                IHello aServiceProxy = anRpcClient.Proxy;

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                //EneterTrace.StartProfiler();

                for (int i = 0; i < 10000; ++i)
                {
                    aServiceProxy.Sum(1, 2);
                }

                //EneterTrace.StopProfiler();

                aStopWatch.Stop();
                Console.WriteLine("Rpc call. Elapsed time = " + aStopWatch.Elapsed);


                HelloService aService = new HelloService();

                Stopwatch aStopWatch2 = new Stopwatch();
                aStopWatch2.Start();

                for (int i = 0; i < 10000; ++i)
                {
                    aService.Sum(1, 2);
                }

                aStopWatch2.Stop();
                Console.WriteLine("Local call. Elapsed time = " + aStopWatch2.Elapsed);
            }
            finally
            {
                if (anRpcClient.IsDuplexOutputChannelAttached)
                {
                    anRpcClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void MultipleClients_RemoteCall_10()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.StartProfiler();

            RpcFactory anRpcFactory = new RpcFactory(mySerializer)
            {
                RpcTimeout = TimeSpan.FromSeconds(30)
            };
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(new HelloService());

            IRpcClient<IHello>[] aClients = new IRpcClient<IHello>[10];
            for (int i = 0; i < aClients.Length; ++i )
            {
                aClients[i] = anRpcFactory.CreateClient<IHello>();
            }

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));

                // Clients open connection.
                foreach(IRpcClient<IHello> aClient in aClients)
                {
                    aClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));
                }

                // Clients communicate with the service in parallel.
                AutoResetEvent aDone = new AutoResetEvent(false);
                int aCounter = 0;
                foreach (IRpcClient<IHello> aClient in aClients)
                {
                    ThreadPool.QueueUserWorkItem(x =>
                        {
                            try
                            {
                                aClient.Proxy.Sum(10, 20);
                                ++aCounter;
                                if (aCounter == aClients.Length)
                                {
                                    aDone.Set();
                                }
                                Thread.Sleep(1);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error("Detected Exception.", err);
                                aDone.Set();
                            }
                        });
                }

                aDone.WaitOne();

                //EneterTrace.StopProfiler();

                Assert.AreEqual(aClients.Length, aCounter);
            }
            finally
            {
                foreach (IRpcClient<IHello> aClient in aClients)
                {
                    aClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif
        
#if !COMPACT_FRAMEWORK
        [Test]
        public void MultipleClients_RemoteEvent_10()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.StartProfiler();

            HelloService aService = new HelloService();
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreateSingleInstanceService<IHello>(aService);

            IRpcClient<IHello>[] aClients = new IRpcClient<IHello>[10];
            for (int i = 0; i < aClients.Length; ++i)
            {
                aClients[i] = anRpcFactory.CreateClient<IHello>();
            }

            try
            {
                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));

                // Clients open connection.
                foreach (IRpcClient<IHello> aClient in aClients)
                {
                    aClient.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));
                }

                // Subscribe to remote event from the service.
                AutoResetEvent anOpenReceived = new AutoResetEvent(false);
                AutoResetEvent aCloseReceived = new AutoResetEvent(false);
                AutoResetEvent anAllCleintsSubscribed = new AutoResetEvent(false);
                int anOpenCounter = 0;
                int aCloseCounter = 0;
                int aSubscribedClientCounter = 0;
                foreach (IRpcClient<IHello> aClient in aClients)
                {
                    IRpcClient<IHello> aClientTmp = aClient;
                    ThreadPool.QueueUserWorkItem(xx =>
                        {
                            aClientTmp.Proxy.Open += (x, y) =>
                                {
                                    ++anOpenCounter;
                                    if (anOpenCounter == aClients.Length)
                                    {
                                        anOpenReceived.Set();
                                    }
                                };

                            aClientTmp.Proxy.Close += (x, y) =>
                                {
                                    ++aCloseCounter;
                                    if (aCloseCounter == aClients.Length)
                                    {
                                        aCloseReceived.Set();
                                    }
                                };

                            ++aSubscribedClientCounter;
                            if (aSubscribedClientCounter == aClients.Length)
                            {
                                anAllCleintsSubscribed.Set();
                            }

                            Thread.Sleep(1);
                        });
                }

                // Wait until all clients are subscribed.
                anAllCleintsSubscribed.WaitOne();

                // Servicde raises two different events.
                OpenArgs anOpenArgs = new OpenArgs()
                {
                    Name = "Hello"
                };
                aService.RaiseOpen(anOpenArgs);
                aService.RaiseClose();


                anOpenReceived.WaitOne();
                aCloseReceived.WaitOne();

            }
            finally
            {
                foreach (IRpcClient<IHello> aClient in aClients)
                {
                    aClient.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NoInterfaceTypeProvided()
        {
            RpcFactory anRpcFactory = new RpcFactory();
            anRpcFactory.CreateClient<OpenArgs>();
        }
#endif

#if !COMPACT_FRAMEWORK
        [Test]
        public void PerClientInstanceService()
        {
            RpcFactory anRpcFactory = new RpcFactory(mySerializer);
            IRpcService<IHello> anRpcService = anRpcFactory.CreatePerClientInstanceService<IHello>(() => new HelloService());
            
            IRpcClient<IHello> anRpcClient1 = anRpcFactory.CreateClient<IHello>();
            IRpcClient<IHello> anRpcClient2 = anRpcFactory.CreateClient<IHello>();

            try
            {
                string aService1IdFromEvent = null;
                anRpcClient1.Proxy.Open += (x, y) =>
                    {
                        aService1IdFromEvent = y.InstanceId;
                    };

                string aService2IdFromEvent = null;
                anRpcClient2.Proxy.Open += (x, y) =>
                    {
                        aService2IdFromEvent = y.InstanceId;
                    };

                anRpcService.AttachDuplexInputChannel(myMessaging.CreateDuplexInputChannel(myChannelId));
                anRpcClient1.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));
                anRpcClient2.AttachDuplexOutputChannel(myMessaging.CreateDuplexOutputChannel(myChannelId));


                string aServiceId1 = anRpcClient1.Proxy.GetInstanceId();
                string aServiceId2 = anRpcClient2.Proxy.GetInstanceId();

                Assert.IsFalse(string.IsNullOrEmpty(aServiceId1));
                Assert.IsFalse(string.IsNullOrEmpty(aService1IdFromEvent));
                Assert.IsFalse(string.IsNullOrEmpty(aServiceId2));
                Assert.IsFalse(string.IsNullOrEmpty(aService2IdFromEvent));
                Assert.AreEqual(aServiceId1, aService1IdFromEvent);
                Assert.AreEqual(aServiceId2, aService2IdFromEvent);
                Assert.AreNotEqual(aServiceId1, aServiceId2);
            }
            finally
            {
                if (anRpcClient1.IsDuplexOutputChannelAttached)
                {
                    anRpcClient1.DetachDuplexOutputChannel();
                }
                if (anRpcClient2.IsDuplexOutputChannelAttached)
                {
                    anRpcClient2.DetachDuplexOutputChannel();
                }

                if (anRpcService.IsDuplexInputChannelAttached)
                {
                    anRpcService.DetachDuplexInputChannel();
                }
            }
        }
#endif

        protected IMessagingSystemFactory myMessaging;
        protected string myChannelId;
        protected ISerializer mySerializer;
    }
}
