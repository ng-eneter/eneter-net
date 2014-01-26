/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

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
    /// Implements the functionality for tracing messages.
    /// </summary>
    /// <remarks>
    /// The EneterTrace allows to trace error messages, warning message, info messages and debug messages.
    /// It also allows to trace entering and leaving from a method and measures the time spent in the method.
    /// In order to trace entering - leaving and debug messages, you must set the detail level to 'Debug'.<br/>
    /// <br/>
    /// Notice: The trace does not display namespaces and method names in Compact Framework platform.
    /// <example>
    /// Tracing entering and leaving and a warning message.
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
    public class EneterTrace : IDisposable
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
        public static IDisposable Entering()
        {
            EneterTrace aTraceObject = null;

            if (DetailLevel == EDetailLevel.Debug)
            {
                aTraceObject = new EneterTrace();

                WriteMessage(ENTERING, null);
            }

#if !COMPACT_FRAMEWORK
            if (DetailLevel == EDetailLevel.Debug || myIsProfilerRunning)
#else
            if (DetailLevel == EDetailLevel.Debug)
#endif
            {
                aTraceObject = new EneterTrace();

#if !SILVERLIGHT && !COMPACT_FRAMEWORK20
                aTraceObject.myStopWatch.Start();
#else
                aTraceObject.myEnteringTime = DateTime.Now;
#endif
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
#if !SILVERLIGHT && !COMPACT_FRAMEWORK20
                if (myStopWatch.IsRunning)
                {
                    myStopWatch.Stop();
                }

                if (DetailLevel == EDetailLevel.Debug)
                {
                    double aMicroseconds = (myStopWatch.Elapsed.TotalMilliseconds - myStopWatch.ElapsedMilliseconds) * 1000;

                    WriteMessage(LEAVING, string.Format(CultureInfo.InvariantCulture, "[{0:D2}:{1:D2}:{2:D2} {3:D3}ms {4:000.0}us]",
                        myStopWatch.Elapsed.Hours,
                        myStopWatch.Elapsed.Minutes,
                        myStopWatch.Elapsed.Seconds,
                        myStopWatch.Elapsed.Milliseconds,
                        aMicroseconds));
                }
#if !COMPACT_FRAMEWORK
                else if (myIsProfilerRunning)
                {
                    UpdateProfiler(myStopWatch.Elapsed.Ticks);
                }
#endif
#else
                if (myEnteringTime != DateTime.MinValue)
                {
                    DateTime aCurrentTime = DateTime.Now;
                    TimeSpan aDuration = aCurrentTime - myEnteringTime;

                    WriteMessage("<--", string.Format(CultureInfo.InvariantCulture, "[{0:D2}:{1:D2}:{2:D2} {3:D3}ms]",
                        aDuration.Hours,
                        aDuration.Minutes,
                        aDuration.Seconds,
                        aDuration.Milliseconds));
                }
#endif
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
                WriteMessage(INFO, message);
            }
        }

        /// <summary>
        /// Traces the information message.
        /// </summary>
        /// <param name="message">info message</param>
        /// <param name="details">additional details</param>
        public static void Info(string message, string details)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                WriteMessage(INFO, message + DETAILS + details);
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
                string aDetails = GetDetailsFromException(err);
                WriteMessage(INFO, message + NEXTLINE + aDetails);
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
                WriteMessage(WARNING, message);
            }
        }

        /// <summary>
        /// Traces the warning message.
        /// </summary>
        /// <param name="message">warning message</param>
        /// <param name="details">additional details</param>
        public static void Warning(string message, string details)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                WriteMessage(WARNING, message + DETAILS + details);
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
                string aDetails = GetDetailsFromException(err);
                WriteMessage(WARNING, message + NEXTLINE + aDetails);
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
                WriteMessage(ERROR, message);
            }
        }

        /// <summary>
        /// Traces the error message and details for the error.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="errorDetails">error details</param>
        public static void Error(string message, string errorDetails)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                WriteMessage(ERROR, message + DETAILS + errorDetails);
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
                string aDetails = GetDetailsFromException(err);
                WriteMessage(ERROR, message + NEXTLINE + aDetails);
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
                WriteMessage(DEBUG, message);
            }
        }

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Starts the profiler measurement.
        /// </summary>
        public static void StartProfiler()
        {
            lock (myProfilerData)
            {
                myIsProfilerRunning = true;
            }
        }
#endif

#if !COMPACT_FRAMEWORK
        /// <summary>
        /// Stops the profiler measurement and writes results to the trace.
        /// </summary>
        public static void StopProfiler()
        {
            // Wait until all items are processed.
            myQueueThreadEndedEvent.WaitOne();

            lock (myProfilerData)
            {
                myIsProfilerRunning = false;

                foreach (KeyValuePair<MethodBase, ProfilerData> anItem in myProfilerData.OrderByDescending(x => x.Value.Ticks))
                {
                    TimeSpan aTimeSpan = TimeSpan.FromTicks(anItem.Value.Ticks);
#if !SILVERLIGHT3 && !WINDOWS_PHONE_70 && !NET35 && !MONO
                    string aMessage = string.Join("", aTimeSpan.ToString(), " ", anItem.Value.Calls, "x ", anItem.Key.ReflectedType.FullName, ".", anItem.Key.Name, "\r\n");
#else
                    string[] aJoinBuf = { aTimeSpan.ToString(), " ", anItem.Value.Calls.ToString(), "x ", anItem.Key.ReflectedType.FullName, ".", anItem.Key.Name };
                    string aMessage = string.Join("", aJoinBuf);
#endif
                    WriteToTrace(aMessage);
                }

                myProfilerData.Clear();
            }
        }
#endif
        
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


#if !COMPACT_FRAMEWORK

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
#endif

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

        private static void WriteMessage(string prefix, string message)
        {
            try
            {
                DateTime aTime = DateTime.Now;

                // Get the calling method
                // Note: We must skip two methods to get the calling method.
                // Note: Be careful which constructor of StackFrame is used because of 'SecurityCriticalAttribute' in Silverlight.
                //       If the attribute is set, then only trusted Silverlight applications can use the functionality.
                //       The current constructor does not have that attribute.
#if !COMPACT_FRAMEWORK
                StackFrame aCallStack = new StackFrame(2);
#endif
                int aThreadId = Thread.CurrentThread.ManagedThreadId;
                Action aTraceJob = () =>
                    {
#if !COMPACT_FRAMEWORK
                        MethodBase aMethod = aCallStack.GetMethod();
                        string[] aJoinBuf = { aMethod.ReflectedType.FullName, aMethod.Name };
                        string aMethodName = string.Join(".", aJoinBuf);

                        // Check the filter.
                        if (myNameSpaceFilter != null && !myNameSpaceFilter.IsMatch(aMethodName))
                        {
                            return;
                        }

                        StringBuilder aMessageBuilder = new StringBuilder();
                        aMessageBuilder.AppendFormat("{0:D2}:", aTime.Hour);
                        aMessageBuilder.AppendFormat("{0:D2}:", aTime.Minute);
                        aMessageBuilder.AppendFormat("{0:D2}.", aTime.Second);
                        aMessageBuilder.AppendFormat("{0:D3}", aTime.Millisecond);
                        aMessageBuilder.AppendFormat(" ~{0:D3}", aThreadId);
                        aJoinBuf = new string[] { aMessageBuilder.ToString(), prefix, aMethodName, message };
#endif

#if COMPACT_FRAMEWORK
                        // Compact framework does not support retrieving the stack :-(.
                        // So the method name is not available but at least the message is traced.
                        StringBuilder aMessageBuilder = new StringBuilder();
                        aMessageBuilder.AppendFormat((IFormatProvider)null,"{0:D2}:", aTime.Hour);
                        aMessageBuilder.AppendFormat((IFormatProvider)null,"{0:D2}:", aTime.Minute);
                        aMessageBuilder.AppendFormat((IFormatProvider)null,"{0:D2}.", aTime.Second);
                        aMessageBuilder.AppendFormat((IFormatProvider)null,"{0:D3}", aTime.Millisecond);
                        aMessageBuilder.AppendFormat((IFormatProvider)null," ~{0:D3}", aThreadId);

                        string[] aJoinBuf = { aMessageBuilder.ToString(), prefix, message };
#endif

                        string aMessage = string.Join(" ", aJoinBuf);

                        // Write the trace message to the buffer.
                        WriteToTraceBuffer(aMessage);
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

#if !COMPACT_FRAMEWORK
        private static void UpdateProfiler(long ticks)
        {
            StackFrame aCallStack = new StackFrame(2);
            MethodBase aMethod = aCallStack.GetMethod();

            Action aProfilerJob = () =>
                {
                    lock (myProfilerData)
                    {
                        ProfilerData aProfileData;
                        myProfilerData.TryGetValue(aMethod, out aProfileData);
                        if (aProfileData == null)
                        {
                            aProfileData = new ProfilerData();
                            myProfilerData[aMethod] = aProfileData;
                        }

                        ++aProfileData.Calls;
                        aProfileData.Ticks += ticks;
                    }
                };

            EnqueueJob(aProfilerJob);
        }
#endif

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
                // If the queue is empty, then start also the thread that will process messages.
                // If the queue is not empty, the processing thread already exists.
                if (myTraceQueue.Count == 0)
                {
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
                        if (myTraceQueue.Count > 0)
                        {
                            aJob = myTraceQueue.Dequeue();
                        }
                        else
                        {
                            return;
                        }
                    }

                    // Execute the job.
                    try
                    {
                        aJob();
                    }
                    catch (Exception err)
                    {
                        // No error should removing jobs from the queue.
                        string anExceptionDetails = GetDetailsFromException(err);
                        Console.WriteLine("EneterTrace failed. " + anExceptionDetails);
                    }

                    lock (myTraceQueue)
                    {
                        if (myTraceQueue.Count == 0)
                        {
                            return;
                        }
                    }
                }
            }
            finally
            {
                myQueueThreadEndedEvent.Set();
            }
        }


        private static void WriteToTraceBuffer(string message)
        {
            lock (myBufferedTraces)
            {
                // If the buffer was empty also start the thread processing the buffer.
                if (myBufferedTraces.Length == 0)
                {
                    // Add the message to the buffer.
                    // Note: compact framework does not support AppendLine therefore the plain Append(...) is used.
                    myBufferedTraces.Append(message);
                    myBufferedTraces.Append("\r\n");

                    // Flush the buffer in the specified time.
                    myTraceBufferFlushTimer.Change(50, -1);
                }
                else
                {
                    // Meanwhile while the flush time is not elapsed continue writing to the buffer.
                    // Note: compact framework does not support AppendLine therefore the plain Append(...) is used.
                    myBufferedTraces.Append(message);
                    myBufferedTraces.Append("\r\n");
                }
            }
        }

        private static void OnFlushTraceBuffer(object x)
        {
            string aBufferedTraceMessages;

            lock (myBufferedTraces)
            {
                aBufferedTraceMessages = myBufferedTraces.ToString();
                
                // Note: compact framework does not support Clear().
                myBufferedTraces.Length = 0;
            }

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
#if !SILVERLIGHT
                        // Otherwise write to the debug port.
                        System.Diagnostics.Debug.Write(message);
#else
                        System.Diagnostics.Debug.WriteLine(message);
#endif
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

#if !SILVERLIGHT && !COMPACT_FRAMEWORK20
        private Stopwatch myStopWatch = new Stopwatch();
#else
        private DateTime myEnteringTime = DateTime.MinValue;
#endif

        // Trace Info, Warning and Error by default.
        private static EDetailLevel myDetailLevel = EDetailLevel.Short;

        private static object myTraceLogLock = new object();
        private static TextWriter myTraceLog;
        
#if !COMPACT_FRAMEWORK        
        private static Regex myNameSpaceFilter;
#endif

        private static ManualResetEvent myQueueThreadEndedEvent = new ManualResetEvent(true);
        private static Queue<Action> myTraceQueue = new Queue<Action>();

        private static StringBuilder myBufferedTraces = new StringBuilder();
        private static Timer myTraceBufferFlushTimer = new Timer(OnFlushTraceBuffer, null, -1, -1);

#if !COMPACT_FRAMEWORK
        private class ProfilerData
        {
            public long Calls;
            public long Ticks;
        }

        private static Dictionary<MethodBase, ProfilerData> myProfilerData = new Dictionary<MethodBase, ProfilerData>();
        private static volatile bool myIsProfilerRunning;
#endif

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
