

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    /// <summary>
    /// Super duper trace.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Example showing how to enable tracing of communication errors and warnings to a file:
    /// <code>
    /// EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Short;
    /// EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
    /// </code>
    /// </example>
    /// <example>
    /// Example showing how to enable tracing of detailed communication sequence to a file:
    /// <code>
    /// EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
    /// EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
    /// </code>
    /// </example>
    /// <example>
    /// Example showing how you can trace entering/leaving methods:
    /// <code>
    /// private class MyClass
    /// {
    ///     private void MyMethod()
    ///     {
    ///         // Tracing entering and leaving the method.
    ///         // Note: The entering-leaving is traced only if detail level is 'Debug'.
    ///         using (EneterTrace.Entering())
    ///         {
    ///             ... method implementation ...
    ///             
    ///             // Tracing a warning message.
    ///             EneterTrace.Warning("This is a warning message.");
    ///             
    ///             ...
    ///         }
    ///     }
    /// }
    /// 
    /// The output:
    /// 11:59:11.365 ~008 --> YourNameSpace.MyClass.MyMethod
    /// 11:59:11.704 ~008  W: YourNameSpace.MyClass.MyMethod This is a warning message.
    /// 11:59:12.371 ~008 &lt;--  YourNameSpace.MyClass.MyMethod [00:00:01 000ms 969.0us]
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class EneterTrace : IDisposable
    {
        /// <summary>
        /// Detail level of the trace.
        /// </summary>
        public enum EDetailLevel
        {
            /// <summary>
            /// Messages are not traced.
            /// </summary>
            None,

            /// <summary>
            /// Info, Warning and Error messages.<br/>
            /// The debug messages and entering-leaving messages are not traced.
            /// </summary>
            Short,

            /// <summary>
            /// All messages are traced.
            /// </summary>
            Debug
        }

        /// <summary>
        /// Traces entering-leaving the method.
        /// </summary>
        /// <remarks>
        /// The enetering information for the method calling this constructor is put to the trace
        /// and the measuring of the time starts.
        /// In order to trace entering-leaving, the detail level must be set to 'Debug'.
        /// </remarks>
        public static IDisposable Entering(string additionalInfo = null)
        {
            EneterTrace aTraceObject = null;

            if (DetailLevel > EDetailLevel.Short || myProfilerIsRunning)
            {
                aTraceObject = new EneterTrace();
                aTraceObject.myCallStack = new StackFrame(1);

                long aEnteringTimeTicks = !myProfilerIsRunning ? DateTime.Now.Ticks : 0;
                aTraceObject.myEnteringTicks = Stopwatch.GetTimestamp();

                if (myProfilerIsRunning)
                {
                    UpdateProfilerForEntering(aTraceObject);
                }
                else
                {
                    WriteMessage(aTraceObject.myCallStack, aEnteringTimeTicks, ENTERING, additionalInfo);
                }
            }

            return aTraceObject;
        }

        /// <summary>
        /// Traces the leaving from the method including the duration time.
        /// </summary>
        void IDisposable.Dispose()
        {
            try
            {
                if (myEnteringTicks != 0)
                {
                    long aLeavingTicks = Stopwatch.GetTimestamp();
                    long aLeavingTimeTicks = !myProfilerIsRunning ? DateTime.Now.Ticks : 0;
                    
                    long aElapsedTicks = aLeavingTicks - myEnteringTicks;

                    if (myProfilerIsRunning)
                    {
                        UpdateProfilerForLeaving(this, aElapsedTicks);
                    }
                    else if (DetailLevel > EDetailLevel.Short)
                    {
                        WriteMessage(myCallStack, aLeavingTimeTicks, LEAVING, null, aElapsedTicks);
                    }
                }
            }
            catch
            {
                // Any exception in this Dispose method is irrelevant.
            }
        }


        /// <summary>
        /// Traces the info message.
        /// </summary>
        /// <param name="message">info message</param>
        public static void Info(string message)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, INFO, message);
            }
        }


        /// <summary>
        /// Traces the info message.
        /// </summary>
        /// <param name="message">info message</param>
        /// <param name="err">exception that will be traced</param>
        public static void Info(string message, Exception err)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                string aDetails = GetDetailsFromException(err);
                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, INFO, message + NEXTLINE + aDetails);
            }
        }

        /// <summary>
        /// Traces warning message.
        /// </summary>
        /// <param name="message">warning message</param>
        public static void Warning(string message)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, WARNING, message);
            }
        }


        /// <summary>
        /// Traces the warning message.
        /// </summary>
        /// <param name="message">warning message</param>
        /// <param name="err">exception that will be traced</param>
        public static void Warning(string message, Exception err)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                string aDetails = GetDetailsFromException(err);
                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, WARNING, message + NEXTLINE + aDetails);
            }
        }

        /// <summary>
        /// Traces the error message.
        /// </summary>
        /// <param name="message">error message</param>
        public static void Error(string message)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, ERROR, message);
            }
        }


        /// <summary>
        /// Traces the error message.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="err">exception that will be traced</param>
        public static void Error(string message, Exception err)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                StackFrame aCallStack = new StackFrame(1);

                string aDetails = GetDetailsFromException(err);
                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, ERROR, message + NEXTLINE + aDetails);
            }
        }

        /// <summary>
        /// Traces the debug message.
        /// </summary>
        /// <remarks>
        /// To trace debug messages, the detail level must be set to debug.
        /// </remarks>
        /// <param name="message">error message</param>
        public static void Debug(string message)
        {
            if (DetailLevel == EDetailLevel.Debug)
            {
                StackFrame aCallStack = new StackFrame(1);

                long aTimeTicks = DateTime.Now.Ticks;
                WriteMessage(aCallStack, aTimeTicks, DEBUG, message);
            }
        }

        /// <summary>
        /// Starts the profiler measurement.
        /// </summary>
        public static void StartProfiler()
        {
            lock (myProfilerData)
            {
                WriteToTrace("Profiler is running...\r\n");
                myProfilerIsRunning = true;
            }
        }

        /// <summary>
        /// Stops the profiler measurement and writes results to the trace.
        /// </summary>
        public static void StopProfiler()
        {
            // Wait until all items are processed.
            myQueueThreadEndedEvent.WaitOne();

            lock (myProfilerData)
            {
                myProfilerIsRunning = false;

                foreach (KeyValuePair<MethodBase, ProfilerData> anItem in myProfilerData.OrderByDescending(x => x.Value.Ticks))
                {
                    string aElapsedTime = TimeTicksToString(anItem.Value.Ticks);
                    string aTimePerCall = TimeTicksToString((long)Math.Round(((double)anItem.Value.Ticks) / anItem.Value.Calls));

                    // Note: .NET35 does not support string.Join with variable arguments.
                    string[] aJoinArgs = { aElapsedTime, " ", anItem.Value.Calls.ToString(), "x |", anItem.Value.MaxConcurency.ToString(), "| #", anItem.Value.MaxRecursion.ToString(), " ", aTimePerCall, " ", anItem.Key.ReflectedType.FullName, ".", anItem.Key.Name, "\r\n" };
                    string aMessage = string.Join("", aJoinArgs);

                    WriteToTrace(aMessage);
                }

                myProfilerData.Clear();

                WriteToTrace("Profiler has ended.\r\n");
            }
        }

        
        /// <summary>
        /// Sets or gets the user defined trace.
        /// </summary>
        /// <remarks>
        /// If the value is set, the trace messages are written to the specified trace and to the debug port.
        /// If the value is null, then messages are written only to the debug port.
        /// </remarks>
        public static TextWriter TraceLog
        {
            get
            {
                lock (myTraceLogLock)
                {
                    return myTraceLog;
                }
            }

            set
            {
                lock (myTraceLogLock)
                {
                    myTraceLog = value;
                }
            }
        }

        /// <summary>
        /// Sets or gets the detail level of the trace.
        /// </summary>
        /// <remarks>
        /// If the detail level is set to 'Short' then only info, warning and error messages are traced.<br/>
        /// If the detail level is set to 'Debug' then all messages are traced.
        /// </remarks>
        public static EDetailLevel DetailLevel { get { return myDetailLevel; } set { myDetailLevel = value; } }


        /// <summary>
        /// Sets or gets the regular expression that will be applied to the namespace to filter traced messages.
        /// </summary>
        /// <remarks>
        /// Sets or gets the regular expression that will be applied to the name space of the traced message.
        /// If the namespace matches with the regular expression, the message will be traced.
        /// If the filter is set to null, then the filter is not used and all messages will be traced.
        /// <example>
        /// The following example shows how to set the filter to trace a certain namespace.
        /// <code>
        /// // Set the debug detailed level.
        /// EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
        /// 
        /// // Examples:
        /// // Traces all name spaces starting with 'My.NameSpace'.
        /// EneterTrace.NameSpaceFilter = new Regex(@"^My\.NameSpace");
        /// 
        /// // Traces exactly the name space 'My.NameSpace'.
        /// EneterTrace.NameSpaceFilter = new Regex(@"^My\.NameSpace$");
        /// 
        /// // Traces name spaces starting with 'Calc.Methods' or 'App.Utilities'.
        /// EneterTrace.NameSpaceFilter = new Regex(@"^Calc\.Methods|^App\.Utilities");
        /// 
        /// // Traces all name spaces except namespaces starting with 'Eneter'.
        /// EneterTrace.NameSpaceFilter = new Regex(@"^(?!\bEneter\b)");
        /// </code>
        /// </example>
        /// </remarks>
        public static Regex NameSpaceFilter
        {
            get
            {
                lock (myTraceLogLock)
                {
                    return myNameSpaceFilter;
                }
            }
            set
            {
                lock (myTraceLogLock)
                {
                    myNameSpaceFilter = value;
                }
            }
        }

        private static string GetDetailsFromException(Exception err)
        {
            // If there is not exception, then return empty string.
            if (err == null)
            {
                return "";
            }

            try
            {
                // Get the exception details.
                StringBuilder aDetails = new StringBuilder();
                aDetails.AppendFormat(CultureInfo.InvariantCulture, "Exception:\r\n{0}: {1}\r\n{2}", err.GetType(), err.Message, err.StackTrace);

                // Get all inner exceptions.
                Exception anInnerException = err.InnerException;
                while (anInnerException != null)
                {
                    aDetails.AppendFormat(CultureInfo.InvariantCulture, "\r\n\r\n{0}: {1}\r\n{2}", anInnerException.GetType(), anInnerException.Message, anInnerException.StackTrace);

                    // Get the next inner exception.
                    anInnerException = anInnerException.InnerException;
                }

                aDetails.Append("\r\n==========\r\n");

                return aDetails.ToString();
            }
            catch (Exception e)
            {
                return "Exception: EneterTrace failed to retrieve excepion details. Message: " + e.Message;
            }
        }

        private static void WriteMessage(StackFrame stack, long timeTicks, string prefix, string message, long elapsedTicks = -1)
        {
            try
            {
                int aThreadId = Thread.CurrentThread.ManagedThreadId;

                Action aTraceJob = () =>
                {
                    MethodBase aMethod = stack.GetMethod();

                    // Check the filter.
                    if (myNameSpaceFilter != null && !myNameSpaceFilter.IsMatch(aMethod.ReflectedType.FullName))
                    {
                        return;
                    }

                    string aTimeStr = timeTicks > -1 ? TimeTicksToString(timeTicks) : null;
                    string aElapsedTicksStr = (elapsedTicks > -1) ? TimeTicksToString(elapsedTicks) : null;

                    string aMessage;

                    // If it is a method leaving.
                    string[] aJoinArgs;
                    if (aElapsedTicksStr != null)
                    {
                        aJoinArgs = new string[] { aTimeStr, " ~", aThreadId.ToString(), " ", prefix, " ", aMethod.ReflectedType.FullName, ".", aMethod.Name, " [", aElapsedTicksStr, "]" };
                    }
                    else
                    {
                        aJoinArgs = new string[] { aTimeStr, " ~", aThreadId.ToString(), " ", prefix, " ", aMethod.ReflectedType.FullName, ".", aMethod.Name, " ", message };
                    }
                    // Note: .NET35 does not support string.Join with variable arguments.
                    aMessage = string.Join("", aJoinArgs);

                    // Write the trace message to the buffer.
                    lock (myTraceBufferLock)
                    {
                        // If the buffer was empty also start the timer processing the buffer.
                        bool aStartTimerFlag = myTraceBuffer.Length == 0;

                        // Add the message to the buffer.
                        myTraceBuffer.AppendLine(aMessage);

                        if (aStartTimerFlag)
                        {
                            // Flush the buffer in the specified time.
                            myTraceBufferFlushTimer.Change(100, -1);
                        }
                    }
                };

                EnqueueJob(aTraceJob);
            }
            catch (Exception err)
            {
                // Note: In case the tracing fails, the error should not be propagated to the application.
                //       Therefore, the exception is ignored.
                string anExceptionDetails = GetDetailsFromException(err);
                Console.WriteLine("EneterTrace failed to trace the message. " + anExceptionDetails);
            }
        }

        private static void UpdateProfilerForEntering(EneterTrace trace)
        {
            int aThreadId = Thread.CurrentThread.ManagedThreadId;

            Action aProfilerJob = () =>
            {
                MethodBase aMethod = trace.myCallStack.GetMethod();

                lock (myProfilerData)
                {
                    ProfilerData aProfileData;
                    myProfilerData.TryGetValue(aMethod, out aProfileData);
                    if (aProfileData == null)
                    {
                        aProfileData = new ProfilerData();
                        aProfileData.Calls = 1;
                        aProfileData.MaxConcurency = 1;
                        aProfileData.MaxRecursion = 1;
                        aProfileData.Threads[aThreadId] = 1;

                        myProfilerData[aMethod] = aProfileData;
                    }
                    else
                    {
                        ++aProfileData.Calls;

                        // If this thread is already inside then it is a recursion.
                        if (aProfileData.Threads.ContainsKey(aThreadId))
                        {
                            int aRecursion = ++aProfileData.Threads[aThreadId];
                            if (aRecursion > aProfileData.MaxRecursion)
                            {
                                aProfileData.MaxRecursion = aRecursion;
                            }
                        }
                        // ... else it is another thread wich is parallel inside.
                        else
                        {
                            aProfileData.Threads[aThreadId] = 1;
                            if (aProfileData.Threads.Count > aProfileData.MaxConcurency)
                            {
                                aProfileData.MaxConcurency = aProfileData.Threads.Count;
                            }
                        }
                    }

                    trace.myBufferedProfileData = aProfileData;
                }
            };

            EnqueueJob(aProfilerJob);
        }

        private static void UpdateProfilerForLeaving(EneterTrace trace, long ticks)
        {
            int aThreadId = Thread.CurrentThread.ManagedThreadId;

            Action aProfilerJob = () =>
            {
                lock (myProfilerData)
                {
                    trace.myBufferedProfileData.Ticks += ticks;
                    int aRecursion = --trace.myBufferedProfileData.Threads[aThreadId];

                    if (aRecursion < 1)
                    {
                        MethodBase aMethod = trace.myCallStack.GetMethod();
                        ProfilerData aProfileData = myProfilerData[aMethod];
                        aProfileData.Threads.Remove(aThreadId);
                    }
                }
            };

            EnqueueJob(aProfilerJob);
        }

        private static string TimeTicksToString(long timeTicks)
        {
            DateTime aElapsedTime = DateTime.FromFileTimeUtc(timeTicks);
            string aResult = aElapsedTime.ToString("HH:mm:ss.ffffff");
            return aResult;
        }

        /// <summary>
        /// Enqueues a job to the queue.
        /// </summary>
        /// <remarks>
        /// The queueing of jobs ensures, the jobs are performed in the correct order
        /// and the writing of the processing does not consume the execution thread.
        /// </remarks>
        /// <param name="job"></param>
        private static void EnqueueJob(Action job)
        {
            lock (myTraceQueue)
            {
                // If the thread processing messages is not running.
                if (!myProcessingIsRunning)
                {
                    myProcessingIsRunning = true;
                    ThreadPool.QueueUserWorkItem(ProcessJobs);
                }

                // Enqueue the trace message.
                myTraceQueue.Enqueue(job);
            }
        }


        /// <summary>
        /// Removes traces from the queue and writes them.
        /// </summary>
        /// <remarks>
        /// The method is executed from a different thread.
        /// The thread then loops until the queue is processed.
        /// </remarks>
        /// <param name="x"></param>
        private static void ProcessJobs(object x)
        {
            myQueueThreadEndedEvent.Reset();

            try
            {
                while (true)
                {
                    Action aJob;
                    lock (myTraceQueue)
                    {
                        if (myTraceQueue.Count == 0)
                        {
                            myProcessingIsRunning = false;
                            return;
                        }

                        aJob = myTraceQueue.Dequeue();
                    }

                    // Execute the job.
                    try
                    {
                        aJob();
                    }
                    catch (Exception err)
                    {
                        string anExceptionDetails = GetDetailsFromException(err);
                        Console.WriteLine("EneterTrace failed. " + anExceptionDetails);
                    }
                }
            }
            finally
            {
                myQueueThreadEndedEvent.Set();
            }
        }


        // Invoked by the timer.
        // Traces are written to the StringBuilder. StringBuilder is flushed once per 50ms.
        private static void OnFlushTraceBufferTick(object x)
        {
            StringBuilder aNewBuffer = new StringBuilder(myTraceBufferCapacity);
            StringBuilder aBufferToFlush;

            // Keep the lock for the shortest possible time.
            lock (myTraceBufferLock)
            {
                aBufferToFlush = myTraceBuffer;
                myTraceBuffer = aNewBuffer;
            }

            string aBufferedTraceMessages = aBufferToFlush.ToString();

            // Flush buffered messages to the trace.
            WriteToTrace(aBufferedTraceMessages);
        }

        private static void WriteToTrace(string message)
        {
            try
            {
                lock (myTraceLogLock)
                {
                    // If a trace log is set, then write to it.
                    if (TraceLog != null)
                    {
                        TraceLog.Write(message);
                        TraceLog.Flush();
                    }
                    else
                    {
                        // Otherwise write to the debug port.
                        System.Diagnostics.Debug.Write(message);
                    }
                }
            }
            catch (Exception err)
            {
                string anExceptionDetails = GetDetailsFromException(err);
                Console.WriteLine("EneterTrace failed to write to the trace." + anExceptionDetails);
            }
        }

        /// <summary>
        /// Private helper constructor.
        /// </summary>
        /// <remarks>
        /// The constructor is private, so the class can be enstantiating only via the 'Entering' method.
        /// </remarks>
        private EneterTrace()
        {
        }

        private long myEnteringTicks;
        private StackFrame myCallStack;
        private ProfilerData myBufferedProfileData;


        // Trace Info, Warning and Error by default.
        private static EDetailLevel myDetailLevel = EDetailLevel.Short;

        private static object myTraceLogLock = new object();
        private static TextWriter myTraceLog;

        private static Regex myNameSpaceFilter;

        private static ManualResetEvent myQueueThreadEndedEvent = new ManualResetEvent(true);
        private static bool myProcessingIsRunning;
        private static Queue<Action> myTraceQueue = new Queue<Action>();

        private static object myTraceBufferLock = new object();
        private static int myTraceBufferCapacity = 10000000;
        private static StringBuilder myTraceBuffer = new StringBuilder();
        private static Timer myTraceBufferFlushTimer = new Timer(OnFlushTraceBufferTick, null, -1, -1);

        private class ProfilerData
        {
            public long Calls;
            public long Ticks;
            public int MaxConcurency;
            public int MaxRecursion;

            public Dictionary<int, int> Threads = new Dictionary<int, int>();
        }

        private static Dictionary<MethodBase, ProfilerData> myProfilerData = new Dictionary<MethodBase, ProfilerData>();
        private static volatile bool myProfilerIsRunning;

        private const string ENTERING = "-->";
        private const string LEAVING = "<--";
        private const string INFO = " I:";
        private const string WARNING = " W:";
        private const string ERROR = " E:";
        private const string DEBUG = " D:";
        private const string NEXTLINE = "\r\n";
        private const string DETAILS = "\r\nDetails: ";

    }
}
