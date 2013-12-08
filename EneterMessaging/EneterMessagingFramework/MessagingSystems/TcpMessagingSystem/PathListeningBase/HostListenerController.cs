/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase
{
    /// <summary>
    /// Single static class ensuring it is possible to register more listeners using same IP address and port
    /// but different paths.
    /// </summary>
    /// <remarks>
    /// E.g.: The user code wants to listen to these 3 addresses:
    /// http://127.0.0.1:9055/aaa/bbb/
    /// http://127.0.0.1:9055/aaa/ccc/
    /// http://127.0.0.1:9055/aaa/ddd/
    /// 
    /// All addresses share the same IP address and port. They are different only in paths.
    /// This class is responsible for maintaining listeners to IP address and port.
    /// So that if the user code wants to register next listener it checks if there is a listener to IP address and port.
    /// If yes, then it registers just the new path.
    /// If not, then it creates the listener to the IP address and port and register there the path.
    /// </remarks>
    internal static class HostListenerController
    {
        public static void StartListening(Uri address,
                                          IHostListenerFactory hostListenerFactory,
                                          object connectionHandler,
                                          ISecurityFactory serverSecurityFactory)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    lock (myListeners)
                    {
                        // Get listener for the address specified in the given uri.
                        HostListenerBase aHostListener = FindHostListener(address);
                        if (aHostListener == null)
                        {
                            // Listener does not exist yet, so create one.
                            IPEndPoint anAddress = GetEndPoint(address);
                            aHostListener = hostListenerFactory.CreateHostListener(anAddress, serverSecurityFactory);

                            myListeners.Add(aHostListener);
                        }
                        else
                        {
                            // If found listener is listening to another protocol.
                            // e.g. if I want to start listening to http but websocket listener is listening on
                            //      the given IP address and port.
                            if (aHostListener.GetType() != hostListenerFactory.ListenerType)
                            {
                                string anErrorMessage = TracedObject + "failed to start " + hostListenerFactory.ListenerType + " because " + aHostListener.GetType() + " is already listening on IP address and port.";
                                EneterTrace.Error(anErrorMessage);
                                throw new InvalidOperationException(anErrorMessage);
                            }
                        }

                        // Register the path listener.
                        aHostListener.RegisterListener(address, connectionHandler);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to start listening.", err);
                    throw;
                }
            }
        }

        public static void StopListening(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    lock (myListeners)
                    {
                        // Get host listener.
                        HostListenerBase aPathListener = FindHostListener(uri);
                        if (aPathListener == null)
                        {
                            return;
                        }

                        // Unregister the path listener.
                        aPathListener.UnregisterListener(uri);

                        // If there is no a path listener then nobody is interested in incoming
                        // HTTP requests and the TCP listening can be stopped.
                        if (aPathListener.ExistAnyListener() == false)
                        {
                            myListeners.Remove(aPathListener);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to stop listening.", err);
                }
            }
        }

        /// <summary>
        /// Returns true if somebody is listening to the given uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsListening(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                lock (myListeners)
                {
                    // Get host listener.
                    HostListenerBase aPathListener = FindHostListener(uri);
                    if (aPathListener == null)
                    {
                        return false;
                    }

                    // If the path listener does not exist then listening is not active.
                    if (aPathListener.ExistListener(uri) == false)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        private static HostListenerBase FindHostListener(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                // Get host listener.
                IPEndPoint anAddress = GetEndPoint(uri);
                HostListenerBase aPathListener = myListeners.FirstOrDefault(x => x.Address.Equals(anAddress));

                return aPathListener;
            }
        }

        private static IPEndPoint GetEndPoint(Uri address)
        {
#if !COMPACT_FRAMEWORK
            IPAddress[] anIpAddresses = Dns.GetHostAddresses(address.Host);
            IPAddress anIpAddress = anIpAddresses[0];
#else
            IPAddress anIpAddress = IPAddress.Parse(address.Host);
#endif
            IPEndPoint anEndPoint = new IPEndPoint(anIpAddress, address.Port);

            return anEndPoint;
        }


        // List of IP address : port listeners. These listeners then maintain particular path listeners.
        private static List<HostListenerBase> myListeners = new List<HostListenerBase>();

        private static string TracedObject { get { return "HostListenerController "; } }
    }
}


#endif