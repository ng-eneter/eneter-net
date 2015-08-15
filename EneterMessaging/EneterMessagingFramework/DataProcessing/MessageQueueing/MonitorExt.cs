/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Threading;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    /// <summary>
    /// Implements missing Wait, Pulse and PulseAll methods for class Monitor in Compact Framework.
    /// </summary>
    internal static class MonitorExt
    {
        public static void Wait(object obj)
        {
            AutoResetEvent aPulseEvent;
            
            using (ThreadLock.Lock(myQueueForPulse)
            {
                // Get the waiting queue for the given obj.
                Queue<AutoResetEvent> aWaitingQueue = null;
                myQueueForPulse.TryGetValue(obj, out aWaitingQueue);
                if (aWaitingQueue == null)
                {
                    aWaitingQueue = new Queue<AutoResetEvent>();
                    myQueueForPulse[obj] = aWaitingQueue;
                }
                
                // Create the pulse event that will indicate this thread shall continue
                // and put it to the queue.
                aPulseEvent = new AutoResetEvent(false);
                aWaitingQueue.Enqueue(aPulseEvent);
            }
            
            // Release the original lock.
            Monitor.Exit(obj);
            
            // Wait until the pulse is called.
            aPulseEvent.WaitOne();
            
            // The thread is released but it must again acquire the original lock.
            Monitor.Enter(obj);
        }
        
        public static bool Wait(object obj, int miliseconds)
        {
            AutoResetEvent aPulseEvent;
            
            using (ThreadLock.Lock(myQueueForPulse)
            {
                // Get the waiting queue for the given obj.
                Queue<AutoResetEvent> aWaitingQueue = null;
                myQueueForPulse.TryGetValue(obj, out aWaitingQueue);
                if (aWaitingQueue == null)
                {
                    aWaitingQueue = new Queue<AutoResetEvent>();
                    myQueueForPulse[obj] = aWaitingQueue;
                }
                
                // Create the pulse event that will indicate this thread shall continue
                // and put it to the queue.
                aPulseEvent = new AutoResetEvent(false);
                aWaitingQueue.Enqueue(aPulseEvent);
            }
            
            // Release the original lock.
            Monitor.Exit(obj);
            
            // Wait until the pulse is called.
            bool wasPulsed = aPulseEvent.WaitOne(miliseconds, false);
            
            // The thread is released but it must again acquire the original lock.
            Monitor.Enter(obj);
            
            return wasPulsed;
        }
        
        public static void Pulse(object obj)
        {
            using (ThreadLock.Lock(myQueueForPulse)
            {
                // Get the waiting queue for the given obj.
                Queue<AutoResetEvent> aWaitingQueue = null;
                myQueueForPulse.TryGetValue(obj, out aWaitingQueue);
                
                // If somebody is waiting for the pulse.
                if (aWaitingQueue != null && aWaitingQueue.Count > 0)
                {
                    // Remove the first waiting thread.
                    AutoResetEvent aPulseEvent = aWaitingQueue.Dequeue();
                    
                    if (aPulseEvent != null)
                    {
                        // Pulse the waiting thread.
                        aPulseEvent.Set();
                    }
                    
                    // If the queue is empty then remove the queue completely.
                    if (aWaitingQueue.Count == 0)
                    {
                        myQueueForPulse.Remove(obj);
                    }
                }
            }
        }
        
        public static void PulseAll(object obj)
        {
            using (ThreadLock.Lock(myQueueForPulse)
            {
                // Get the waiting queue for the given obj.
                Queue<AutoResetEvent> aWaitingQueue = null;
                myQueueForPulse.TryGetValue(obj, out aWaitingQueue);
                
                // If somebody is waiting for the pulse.
                if (aWaitingQueue != null && aWaitingQueue.Count > 0)
                {
                    // Pulse all waiting threads.
                    foreach (AutoResetEvent aPulseEvent in aWaitingQueue)
                    {
                        if (aPulseEvent != null)
                        {
                            // Pulse the waiting thread.
                            aPulseEvent.Set();
                        }
                    }

                    // All waiting thread were pulsed, so the queue can be removed.
                    myQueueForPulse.Remove(obj);
                }
            }
        }
        
        private static Dictionary<object, Queue<AutoResetEvent>> myQueueForPulse = new Dictionary<object, Queue<AutoResetEvent>>();
    }
}

#endif