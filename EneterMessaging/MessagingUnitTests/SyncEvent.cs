using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Eneter.MessagingUnitTests
{
    public static class SyncEvent
    {
        public static bool WaitFor(Func<bool> condition, TimeSpan maxWaitingTime)
        {
            DateTime aStartingTime = DateTime.Now;

            while (true)
            {
                if (condition())
                {
                    return true;
                }

                if (DateTime.Now - aStartingTime > maxWaitingTime)
                {
                    return false;
                }

                Thread.Sleep(80);
            }

            

        }
    }
}
