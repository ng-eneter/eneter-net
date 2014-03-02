/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

using System;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    internal class SingleThreadExecutor
    {
        public SingleThreadExecutor()
        {
            myWorkingThread = new Thread(DoJobs);
            myWorkingThread.IsBackground = true;
            myWorkingThread.Start();

            // Store the tracing info.
            TracedObject = "TO ~ " + myWorkingThread.ManagedThreadId;
        }

        public void Execute(Action job)
        {
            EneterTrace.Debug(TracedObject);
            myJobQueue.EnqueueMessage(job);
        }

        private void DoJobs()
        {
            while (true)
            {
                Action aJob = myJobQueue.DequeueMessage();

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

        private MessageQueue<Action> myJobQueue = new MessageQueue<Action>();
        private Thread myWorkingThread;
        
        private string TracedObject;
    }
}
