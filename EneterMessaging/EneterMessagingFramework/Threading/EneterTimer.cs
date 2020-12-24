

using System.Threading;

namespace Eneter.Messaging.Threading
{
	internal class EneterTimer
	{
		public EneterTimer(TimerCallback callback)
		{
            myTickCallback = callback;
		}

		public void Change(int millisecondsTimeout)
		{
            //using (EneterTrace.Entering())
            //{
                lock (myScheduleLock)
                {
                    // Release the previous waiting.
                    myTimeElapsedEvent.Set();

                    if (millisecondsTimeout > -1)
                    {
                        // Set the new waiting.
                        myMillisecondsTimeout = millisecondsTimeout;
                        myWaitingThreadPool.Execute(DoWaiting);

                        // Wait until the waiting started.
                        // This is to avoid mulitple waitings cumulated the queue.
                        myWaitingStartedEvent.WaitOne(5000);
                    }
                }
            //}
		}

		private void DoWaiting()
		{
            //using (EneterTrace.Entering())
            //{
                // Indicate the waiting started.
                myWaitingStartedEvent.Set();

                // Wait until the time elapses or until the waiting is canceled.
                myTimeElapsedEvent.Reset();
                if (myTimeElapsedEvent.WaitOne(myMillisecondsTimeout))
                {
                    // if the waiting is canceled then just return.
                    return;
                }

                // The time ellapsed so call the tick callback.
                // Execute it in a different thread so that if the tick-callback calls EneterTimer.Change
                // the deadlock will not happen.
                myTickingThreadPool.Execute(() => myTickCallback(null));
            //}
		}

		private TimerCallback myTickCallback;
		private int myMillisecondsTimeout;
        private ManualResetEvent myTimeElapsedEvent = new ManualResetEvent(false);
        private AutoResetEvent myWaitingStartedEvent = new AutoResetEvent(false);
        private object myScheduleLock = new object();
        private ScalableThreadPool myWaitingThreadPool = new ScalableThreadPool(0, 1, 1000);
        private ScalableThreadPool myTickingThreadPool = new ScalableThreadPool(0, 100, 10000);

	}
}
