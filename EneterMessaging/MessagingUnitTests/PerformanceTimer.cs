using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Eneter.MessagingUnitTests
{
    internal class PerformanceTimer
    {
        public void Start()
        {
            myStopWatch.Start();
        }

        public void Stop()
        {
            myStopWatch.Stop();
            Console.WriteLine("Elapsed time = " + myStopWatch.Elapsed);
        }

        private Stopwatch myStopWatch = new Stopwatch();
    }
}
