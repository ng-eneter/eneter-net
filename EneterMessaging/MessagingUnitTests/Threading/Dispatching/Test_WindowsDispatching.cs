#if !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Windows.Threading;
using System.Threading;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.MessagingUnitTests.Threading.Dispatching
{
    [TestFixture]
    public class Test_WindowsDispatching
    {
        [Test]
        public void AttachToDispatcher()
        {
            Dispatcher aDispatcher = WindowsDispatching.StartNewWindowsDispatcher();

            IThreadDispatcherProvider aDispatching = new WindowsDispatching(aDispatcher);
            IThreadDispatcher anEneterDispatcher = aDispatching.GetDispatcher();

            ManualResetEvent anInvokeCompleted1 = new ManualResetEvent(false);
            int aThreadId1 = 0;
            anEneterDispatcher.Invoke(() =>
                {
                    aThreadId1 = Thread.CurrentThread.ManagedThreadId;
                    anInvokeCompleted1.Set();
                });
            anInvokeCompleted1.WaitOne();

            ManualResetEvent anInvokeCompleted2 = new ManualResetEvent(false);
            int aThreadId2 = 0;
            anEneterDispatcher.Invoke(() =>
                {
                    aThreadId2 = Thread.CurrentThread.ManagedThreadId;
                    anInvokeCompleted2.Set();
                });
            anInvokeCompleted1.WaitOne();

            WindowsDispatching.StopWindowsDispatcher(aDispatcher);

            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, aThreadId1);
            Assert.AreEqual(aThreadId1, aThreadId2);
        }


        
    }
}

#endif