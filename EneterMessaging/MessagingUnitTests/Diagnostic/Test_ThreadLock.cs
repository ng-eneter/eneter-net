using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.Diagnostic
{
    [TestFixture]
    public class Test_ThreadLock
    {
        [Test]
        public void PerformanceTest()
        {
            PerformanceTimer aTimer = new PerformanceTimer();

            ManualResetEvent aCompleted = new ManualResetEvent(false);
            object aLock = new object();
            int aSum = 0;


            // Using synchronized keyword
            aTimer.Start();

            for (int i = 0; i < 10; ++i)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    for (int j = 0; j < 1000000; ++j)
                    {
                        lock (aLock)
                        {
                            ++aSum;
                            if (aSum == 10000000)
                            {
                                aCompleted.Set();
                            }
                        }
                    }
                });
            }

            aCompleted.WaitOne();
            aTimer.Stop();


            // Using ThreadLock
            aCompleted.Reset();
            aTimer.Start();
            aSum = 0;

            for (int i = 0; i < 10; ++i)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    for (int j = 0; j < 1000000; ++j)
                    {
                        using (ThreadLock.Lock(aLock))
                        {
                            ++aSum;
                            if (aSum == 10000000)
                            {
                                aCompleted.Set();
                            }
                        }
                    }
                });
            }

            aCompleted.WaitOne();
            aTimer.Stop();
        }

    }
}
