/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase
{
    /// <summary>
    /// Base class representing listeners listening to the particular IP address and port and
    /// forwarding the processing according to path to the correct handler.
    /// E.g. http://127.0.0.1:9055/aaa/bbb/.
    /// The Host listener is listening to 127.0.0.1:9055. Then it parse out the path /aaa/bbb/ and
    /// forwards the request to the handler responsible for this path.
    /// </summary>
    /// <remarks>
    /// This is used for the implementation of http and websocket listeners.
    /// </remarks>
    internal abstract class HostListenerBase
    {
        public HostListenerBase(IPEndPoint address, ISecurityFactory securityFactory)
        {
            Address = address;
            myTcpListener = new TcpListenerProvider(address);
            SecurityFactory = securityFactory;
        }

        public void RegisterListener(Uri address, object processConnection)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myHandlers))
                {
                    // If the path listener already exists then error, because only one instance can listen.
                    if (myHandlers.Any(x => x.Key.AbsolutePath == address.AbsolutePath))
                    {
                        // The listener already exists.
                        string anErrorMessage = TracedObject + "detected the address is already used.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }


                    // Add handler for this path.
                    myHandlers.Add(new KeyValuePair<Uri, object>(address, processConnection));

                    // If the host listener does not listen to sockets yet, then start it.
                    if (myTcpListener.IsListening == false)
                    {
                        try
                        {
                            myTcpListener.StartListening(HandleConnection);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to start the path listener.", err);

                            UnregisterListener(address);

                            throw;
                        }
                    }
                }
            }
        }

        public void UnregisterListener(Uri address)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myHandlers))
                    {
                        // Remove handler for that path.
                        myHandlers.RemoveWhere(x => x.Key.AbsolutePath == address.AbsolutePath);
                    
                        // If there is no the end point then nobody is handling messages and the listening can be stopped.
                        if (myHandlers.Count == 0)
                        {
                            myTcpListener.StopListening();
                        }
                    }
                }
                catch (Exception err)
                {
                    String anErrorMessage = TracedObject + "failed to unregister path-listener.";
                    EneterTrace.Warning(anErrorMessage, err);
                }
            }
                
        }

        public bool ExistListener(Uri address)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myHandlers))
                {
                    bool isAny = myHandlers.Any(x => x.Key.AbsolutePath == address.AbsolutePath);
                    return isAny;
                }
            }
        }

        public bool ExistAnyListener()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myHandlers))
                {
                    return myHandlers.Count > 0;
                }
            }
        }

        protected abstract void HandleConnection(TcpClient tcpClient);

        public IPEndPoint Address { get; private set; }

        protected ISecurityFactory SecurityFactory { get; private set; }
        private TcpListenerProvider myTcpListener;

        protected HashSet<KeyValuePair<Uri, object>> myHandlers = new HashSet<KeyValuePair<Uri, object>>();
        protected abstract string TracedObject { get; }
    }
}


#endif