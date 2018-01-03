/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2016
*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace Eneter.Messaging.Threading
{
    class ScalableThreadPool
    {
        // Synchronized queue for tasks.
        private class TaskQueue
        {
            public bool Enqueue(Action task)
            {
                //using (EneterTrace.Entering())
                //{
                    // Wait until lock is acquired.
                    lock (myTasks)
                    {
                        // Put task to the queue.
                        myTasks.Enqueue(task);

                        // Check if there is a thread(s) waiting until a new task is entered.
                        bool anIdleThreadExist = myNumberOfIdleThreads > 0;

                        // Release the lock and signal the new task was entered.
                        Monitor.Pulse(myTasks);
                        return anIdleThreadExist;
                    }
                //}
            }

            public Action Dequeue(int timeout)
            {
                //using (EneterTrace.Entering())
                //{
                    // Wait until lock is acquired.
                    lock (myTasks)
                    {
                        // If the queue with task is not empty then remove the first one and process it.
                        if (myTasks.Count > 0)
                        {
                            return myTasks.Dequeue();
                        }

                        // The queue with tasks is empty so return the lock and wait until
                        // a task is entered and the lock is acquired again or until timeout.
                        ++myNumberOfIdleThreads;

                        // Release the lock and signal the new task was entered.
                        if (!Monitor.Wait(myTasks, timeout))
                        {
                            // If the lock is not reacquired.
                            --myNumberOfIdleThreads;
                            return null;
                        }

                        // The lock is reacquired. It means something is in the queue.
                        // Note: but it happened the queue was empty and the exception was thrown
                        //       so the check if the queue is empty is still needed.
                        --myNumberOfIdleThreads;
                        if (myTasks.Count > 0)
                        {

                            return myTasks.Dequeue();
                        }
                        else
                        {
                            return null;
                        }
                    }
                //}
            }


            private int myNumberOfIdleThreads;
            private Queue<Action> myTasks = new Queue<Action>();
        }

        // Thread living in the pool.
        private class PoolThread
        {
            public PoolThread(ScalableThreadPool threadPool)
            {
                myThreadPool = threadPool;
                myThread = new Thread(Run);
                myThread.IsBackground = true;
				myThread.Name = threadPool.myThreadsName;
            }

            public void start()
            {
                myThread.Start();
            }

            private void Run()
            {
                while (true)
                {
                    Action aTask = myThreadPool.myTaskQueue.Dequeue(myThreadPool.myMaxIdleTime);
                    if (aTask != null)
                    {
                        try
                        {
                            aTask();
                        }
                        catch
                        {
                            // If task throws an exception then do not care beacause the thread pool needs to continue.
                        }
                    }
                    else
                    {
                        lock (myThreadPool.myNumberOfThreadsManipulator)
                        {
                            if (myThreadPool.myNumberOfThreads > myThreadPool.myMinNumberOfThreads)
                            {
                                --myThreadPool.myNumberOfThreads;
                                break;
                            }
                        }
                    }
                }
            }

            private ScalableThreadPool myThreadPool;
            private Thread myThread;
        }

		public ScalableThreadPool(int minThreads, int maxThreads, int maxIdleTime)
			: this(minThreads, maxThreads, maxIdleTime, "ScalableThreadPool")
		{
		}

		public ScalableThreadPool(int minThreads, int maxThreads, int maxIdleTime, string threadsName)
        {
            myMinNumberOfThreads = minThreads;
            myMaxNumberOfThreads = maxThreads;
            myMaxIdleTime = maxIdleTime;
			myThreadsName = threadsName;
        }

        public void Execute(Action task)
        {
            // Enqueue the task.
            bool anIdleThreadExist = myTaskQueue.Enqueue(task);

            // If there is no free idle thread then start one if it does not exceed number of maximum threads.
            if (!anIdleThreadExist)
            {
                lock (myNumberOfThreadsManipulator)
                {
                    if (myNumberOfThreads < myMaxNumberOfThreads)
                    {
                        ++myNumberOfThreads;

                        PoolThread aThread = new PoolThread(this);
                        aThread.start();
                    }
                }
            }
        }


        private int myMinNumberOfThreads;
        private int myMaxNumberOfThreads;
        private int myMaxIdleTime;
		private string myThreadsName;

        private object myNumberOfThreadsManipulator = new object();
        private int myNumberOfThreads;

        private TaskQueue myTaskQueue = new TaskQueue();
    }
}
