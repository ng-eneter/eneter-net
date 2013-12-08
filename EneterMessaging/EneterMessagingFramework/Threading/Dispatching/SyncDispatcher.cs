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

namespace Eneter.Messaging.Threading.Dispatching
{
    internal class SyncDispatcher : IDispatcher
    {
        public void Invoke(Action workItem)
        {
            using (EneterTrace.Entering())
            {
                lock (myQueue)
                {
                    // If the queue is empty, then start also the thread that will process messages.
                    // If the queue is not empty, the processing thread already exists.
                    if (myQueue.Count == 0)
                    {
                        ThreadPool.QueueUserWorkItem(DoWork);
                    }

                    // Enqueue the action to be executed.
                    myQueue.Enqueue(workItem);
                }
            }
        }

        private void DoWork(object x)
        {
            using (EneterTrace.Entering())
            {
                using (EneterTrace.Entering())
                {
                    try
                    {
                        Action aJob;

                        while (true)
                        {
                            // Take the job from the queue.
                            lock (myQueue)
                            {
                                if (myQueue.Count == 0)
                                {
                                    return;
                                }

                                aJob = myQueue.Dequeue();
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

                            // If it was the last job then the working thread can end.
                            lock (myQueue)
                            {
                                if (myQueue.Count == 0)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed in processing the working queue.", err);
                    }
                }
            }
        }


        private Queue<Action> myQueue = new Queue<Action>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
