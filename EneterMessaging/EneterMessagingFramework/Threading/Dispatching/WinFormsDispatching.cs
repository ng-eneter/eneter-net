/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/


#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using System;
using System.ComponentModel;

#if COMPACT_FRAMEWORK
using System.Windows.Forms;
#endif

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Invokes to WinForms main UI thread.
    /// </summary>
    public class WinFormsDispatching : IThreadDispatcherProvider
    {
        private class WinFormDispatcher : IThreadDispatcher
        {
#if !COMPACT_FRAMEWORK
            private ISynchronizeInvoke myDispatcher;
#else
            private Control myDispatcher;
#endif

#if !COMPACT_FRAMEWORK
            public WinFormDispatcher(ISynchronizeInvoke dispatcher)
#else
            public WinFormDispatcher(Control dispatcher)
#endif
            {
                myDispatcher = dispatcher;
            }

            public void Invoke(Action workItem)
            {
                using (EneterTrace.Entering())
                {
                    if (myDispatcher.InvokeRequired)
                    {
                        myDispatcher.Invoke(workItem, null);
                    }
                    else
                    {
                        workItem.Invoke();
                    }
                }
            }
        }

        private WinFormDispatcher myDispatcher;

        /// <summary>
        /// Constructs the dispatcher provider.
        /// </summary>
        /// <param name="dispatcher">UI control e.g. WinForm which represents the thread where invokes shall be routed.</param>
#if !COMPACT_FRAMEWORK
            public WinFormsDispatching(ISynchronizeInvoke dispatcher)
#else
            public WinFormsDispatching(Control dispatcher)
#endif
        {
            myDispatcher = new WinFormDispatcher(dispatcher);
        }

        /// <summary>
        /// Returns the thread dispatcher which routes invokactions into the WinForms UI thread.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            return myDispatcher;
        }
    }

}

#endif