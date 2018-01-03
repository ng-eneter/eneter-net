/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2018
*/

using System;

namespace Eneter.Messaging.Threading
{
    internal class EneterThreadPool
    {
        public static void QueueUserWorkItem(Action callback)
        {
            myThreadPool.Execute(callback);
        }

        private static ScalableThreadPool myThreadPool = new ScalableThreadPool(10, int.MaxValue, 5000);
    }
}
