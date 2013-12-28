/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if SILVERLIGHT

using System;
using System.Windows;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that invokes incoming methods in the Silverlight thread.
    /// </summary>
    public class SilverlightDispatching : IThreadDispatcherProvider
    {
        private class SilverlightDispatcher : IThreadDispatcher
        {
            public void Invoke(Action workItem)
            {
                using (EneterTrace.Entering())
                {
                    Deployment.Current.Dispatcher.BeginInvoke(workItem);
                }
            }
        }

        /// <summary>
        /// Returns dispatcher which invokes methods in the Silverlight thread.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            using (EneterTrace.Entering())
            {
                return myDispatcher;
            }
        }


        private SilverlightDispatcher myDispatcher = new SilverlightDispatcher();
    }
}


#endif