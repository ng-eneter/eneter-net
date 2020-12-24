



using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase
{
    /// <summary>
    /// Base class representing listeners listening on IP address, port and path.
    /// E.g. websockets and http
    /// </summary>
    /// <typeparam name="_TClientContext"></typeparam>
    internal abstract class PathListenerProviderBase<_TClientContext>
    {
        public PathListenerProviderBase(IHostListenerFactory hostListenerFactory, Uri uri)
            : this(hostListenerFactory, uri, new NonSecurityFactory())
        {
        }

        public PathListenerProviderBase(IHostListenerFactory hostListenerFactory, Uri uri, ISecurityFactory securityFactory)
        {
            using (EneterTrace.Entering())
            {
                myHostListenerFactory = hostListenerFactory;
                Address = uri;
                mySecurityFactory = securityFactory;
            }
        }

        public void StartListening(Action<_TClientContext> connectionHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        if (IsListening)
                        {
                            string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                            EneterTrace.Error(aMessage);
                            throw new InvalidOperationException(aMessage);
                        }

                        if (connectionHandler == null)
                        {
                            throw new ArgumentNullException("The input parameter connectionHandler is null.");
                        }

                        myConnectionHandler = connectionHandler;

                        HostListenerController.StartListening(Address, myHostListenerFactory, myConnectionHandler, mySecurityFactory);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
                    throw;
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        HostListenerController.StopListening(Address);
                        myConnectionHandler = null;
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return HostListenerController.IsListening(Address);
                    }
                }
            }
        }

        public Uri Address { get; private set; }


        private IHostListenerFactory myHostListenerFactory;
        private Action<_TClientContext> myConnectionHandler;
        private ISecurityFactory mySecurityFactory;
        private object myListeningManipulatorLock = new object();


        protected abstract string TracedObject { get; }
    }
}