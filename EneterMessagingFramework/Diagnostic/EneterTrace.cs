/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    /// In order to trace entering - leaving and debug messages, you must set the detail level to 'Debug'.
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
            /// All messages.
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

                WriteMessage("-->", null);

#if !SILVERLIGHT
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
#if !SILVERLIGHT
                if (myStopWatch.IsRunning)
                {
                    myStopWatch.Stop();

                    double aMicroseconds = (myStopWatch.Elapsed.TotalMilliseconds - myStopWatch.ElapsedMilliseconds) * 1000;

                    WriteMessage("<--", string.Format(CultureInfo.InvariantCulture, "[{0:D2}:{1:D2}:{2:D2} {3:D3}ms {4:000.0}us]",
                        myStopWatch.Elapsed.Hours,
                        myStopWatch.Elapsed.Minutes,
                        myStopWatch.Elapsed.Seconds,
                        myStopWatch.Elapsed.Milliseconds,
                        aMicroseconds));
                }
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
                WriteMessage(" I:", message);
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
                WriteMessage(" I:", message + "\r\nDetails: " + details);
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
                WriteMessage(" I:", message + "\r\n" + aDetails);
            }
        }

        /// <summary>
        /// Traces warning message.
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message)
        {
            if (DetailLevel != EDetailLevel.None)
            {
                WriteMessage(" W:", message);
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
                WriteMessage(" W:", message + "\r\nDetails: " + details);
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
                WriteMessage(" W:", message + "\r\n" + aDetails);
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
                WriteMessage(" E:", message);
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
                WriteMessage(" E:", message + "\r\nDetails: " + errorDetails);
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
                WriteMessage(" E:", message + "\r\n" + aDetails);
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
                WriteMessage(" D:", message);
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


        private static void WriteMessage(string prefix, string message)
        {
            DateTime aTime = DateTime.Now;

            // Get the calling method
            // Note: We must skip two stack frames to be set on the calling method.
            StackFrame aCallStack = new StackFrame(2, false);
            MethodBase aMethod = aCallStack.GetMethod();
            string aMethodName = aMethod.ReflectedType.FullName + "." + aMethod.Name;

            string aMessage = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3} ~{4:D3} {5} {6} {7}",
                aTime.Hour, aTime.Minute, aTime.Second, aTime.Millisecond,
                Thread.CurrentThread.ManagedThreadId,
                prefix, aMethodName, message);

            // Writing to the log is performed in another thread to minimize the impact on the performance.
            WaitCallback aDoWrite = x =>
                {
                    lock (myTraceLogLock)
                    {
                        // Check if the message matches with the filter.
                        // Note: If the filter is not set or string matches.
                        if (myNameSpaceFilter == null || myNameSpaceFilter.IsMatch(aMethodName))
                        {
                            // If a trace log is set, then write to it.
                            if (TraceLog != null)
                            {
                                TraceLog.WriteLine(aMessage);
                                TraceLog.Flush();
                            }
                            else
                            {
                                // Otherwise write to the debug port.
                                System.Diagnostics.Debug.WriteLine(aMessage);
                            }
                        }
                    }
                };

            ThreadPool.QueueUserWorkItem(aDoWrite);
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

#if !SILVERLIGHT
        private Stopwatch myStopWatch = new Stopwatch();
#else
        private DateTime myEnteringTime = DateTime.MinValue;
#endif

        // Trace Info, Warning and Error by default.
        private static EDetailLevel myDetailLevel = EDetailLevel.Short;

        private static object myTraceLogLock = new object();
        private static TextWriter myTraceLog;
        private static Regex myNameSpaceFilter;
    }
}
