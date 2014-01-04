
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using NUnit.Framework;
using System.Diagnostics;

namespace Eneter.MessagingUnitTests.Diagnostic
{
    [TestFixture]
    public class Test_EneterTrace
    {
        [Test]
        public void TraceMessages()
        {
            EneterTrace.Info("This is info.");
            EneterTrace.Warning("This is warning.");
            EneterTrace.Error("This is error.");
            EneterTrace.Error("This is error.", "detail error info");

            // Trace exception
            try
            {
                try
                {
                    try
                    {
                        TestMethod1();
                    }
                    catch (Exception err)
                    {
                        throw new Exception("2nd Inner Exception.", err);
                    }
                }
                catch (Exception err)
                {
                    throw new Exception("3th Inner Exception.", err);
                }
            }
            catch (Exception err)
            {
                EneterTrace.Info("Info with exception", err);
                EneterTrace.Warning("Warning with exception", err);
                EneterTrace.Error("Error with exception", err);
            }

            EneterTrace.Error("This is the error with Null", (Exception)null);

            try
            {
                EneterTrace.TraceLog = Console.Out;

                EneterTrace.Info("Info also to console.");
                EneterTrace.Warning("Warning also to console.");
                EneterTrace.Error("Error also to console.");
            }
            finally
            {
                EneterTrace.TraceLog = null;
            }

            // Give some time allowing the trace buffer to flush.
            Thread.Sleep(300);
        }

        [Test]
        public void EneterExitMethodTrace()
        {
            //EneterTrace.TraceLog = Console.Out;
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            using (EneterTrace.Entering())
            {
                //EneterTrace.Info("Hello");
                Thread.Sleep(1000);
            }

            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Short;
            EneterTrace.TraceLog = null;
        }

        [Test]
        public void TraceMessagesMultithread()
        {
            List<Thread> aThreads = new List<Thread>();
            for (int i = 0; i < 10; ++i)
            {
                Thread aThread = new Thread(() =>
                    {
                        TraceMessages();
                        Thread.Sleep(1);
                    });

                aThreads.Add(aThread);
            }
            aThreads.ForEach(x => x.Start());

            aThreads.ForEach(x => Assert.IsTrue(x.Join(300)));
        }

#if !COMPACT_FRAMEWORK
        [Test]
        public void FilterTest()
        {
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            
            // Write traces to the string.
            EneterTrace.TraceLog = new StringWriter();

            try
            {
                // Eneter trace.
                EneterTrace.NameSpaceFilter = new Regex("^Eneter");
                EneterTrace.Debug("This message shall be traced.");
                Thread.Sleep(100);
                string aMessage = EneterTrace.TraceLog.ToString();
                Assert.IsTrue(aMessage.Contains("This message shall be traced."));


                // Create the new "log".
                EneterTrace.TraceLog = new StringWriter();

                // Eneter trace shall be filtered out.
                EneterTrace.NameSpaceFilter = new Regex(@"^(?!\bEneter\b)");
                EneterTrace.Debug("This message shall not be traced.");
                Thread.Sleep(100);
                Assert.AreEqual("", EneterTrace.TraceLog.ToString());
            }
            finally
            {
                EneterTrace.TraceLog = null;
                EneterTrace.NameSpaceFilter = null;
                EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Short;
            }
        }
#endif

#if !COMPACT_FRAMEWORK

        [Test]
	    public void performanceTest()
	    {
	        EneterTrace.EDetailLevel aStoredDetailedLevel = EneterTrace.DetailLevel;
	        TextWriter aStoredTraceLog = EneterTrace.TraceLog;
	    
	        try
	        {
    	        // Without tracing.
    	        EneterTrace.DetailLevel = EneterTrace.EDetailLevel.None;
    	        Stopwatch aStopWatch1 = new Stopwatch();
                aStopWatch1.Start();
                calculatePi();
                aStopWatch1.Stop();
    	        TimeSpan aDeltaTime1 = aStopWatch1.Elapsed;
    	    
    	    
    	        // With traceing.
    	        EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
                EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
    	    
                Stopwatch aStopWatch2 = new Stopwatch();
                aStopWatch2.Start();
                calculatePi();
                aStopWatch2.Stop();
    	        TimeSpan aDeltaTime2 = aStopWatch2.Elapsed;
            
                Console.WriteLine("No trace: " + aDeltaTime1);
                Console.WriteLine("With trace: " + aDeltaTime2);
	        }
	        finally
	        {
                EneterTrace.DetailLevel = aStoredDetailedLevel;
                EneterTrace.TraceLog = aStoredTraceLog;
	        }
	    }
#endif

        
        private void TestMethod1()
        {
            TestMethod2();
        }

        private void TestMethod2()
        {
            TestMethod3();
        }

        private void TestMethod3()
        {
            throw new InvalidOperationException("1st Inner Exception.");
        }

        private void calculatePi()
	{
	    using (EneterTrace.Entering())
        {
            double aCalculatedPi = 0.0;
            for (double i = -1.0; i <= 1.0; i += 0.005)
            {
                aCalculatedPi += calculateRange(i, i + 0.005);
            }
     
            Console.WriteLine("PI = " + aCalculatedPi.ToString());
        }
	}

        private double calculateRange(double from, double to)
        {
            using (EneterTrace.Entering())
            {
                // Calculate pi
                double aResult = 0.0;
                double aDx = 0.00001;
                for (double x = from; x < to; x += aDx)
                {
                    EneterTrace.Debug("blblblblblblblblblblblblbbllblbblblblblbl");
                    aResult += 2 * Math.Sqrt(1 - x * x) * aDx;
                }

                return aResult;
            }
        }
    }
}

