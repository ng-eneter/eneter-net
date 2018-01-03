/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    internal class SingleThreadExecutor
    {
        public void Execute(Action job)
        {
            using (ThreadLock.Lock(myJobQueue))
            {
                if (!myIsWorkingThreadRunning)
                {
                    myIsWorkingThreadRunning = true;
                    EneterThreadPool.QueueUserWorkItem(DoJobs);

                    // If we are tracing then wait until the message about the working thread is ready.
                    if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                    {
                        myTraceMessageReady.WaitOne();
                    }
                }

                // Trace into which thread it forwards the job.
                EneterTrace.Debug(myInvokeTraceMessage);

                myJobQueue.Enqueue(job);
            }
        }

        private void DoJobs()
        {
            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                myInvokeTraceMessage = "TO ~" + Thread.CurrentThread.ManagedThreadId;
                myTraceMessageReady.Set();
            }

            while (true)
            {
                Action aJob;
                using (ThreadLock.Lock(myJobQueue))
                {
                    if (myJobQueue.Count == 0)
                    {
                        myIsWorkingThreadRunning = false;
                        return;
                    }

                    aJob = myJobQueue.Dequeue();
                }

                try
                {
                    // Execute the job.
                    aJob();
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private Queue<Action> myJobQueue = new Queue<Action>();
        private bool myIsWorkingThreadRunning;

        private string myInvokeTraceMessage;
        private AutoResetEvent myTraceMessageReady = new AutoResetEvent(false);
        private string TracedObject { get { return GetType().Name; } }
    }
}
