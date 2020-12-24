

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
        /// <summary>
        /// Starts listening for the given URI path.
        /// </summary>
        /// <remarks>
        /// The listening consists of two parts:
        /// => Host listener - TCP listening on an address and port.
        /// => Path listener - based on the above protocol (HTTP or WebSocket) listening to the path.
        /// 
        /// If the URI contains hostname instead of the IP address then it resolves the host name.
        /// But the result can be multiple addresses. E.g. for localhost it can return IPV4: 127.0.0.1 and IPV6: [::1].
        /// In sach case it will try to start listening to all addresses associated with the host name.
        /// If start listening fails for one of those addresses then StartListening throws exception.
        /// 
        /// </remarks>
        /// <param name="address"></param>
        /// <param name="hostListenerFactory"></param>
        /// <param name="connectionHandler"></param>
        /// <param name="serverSecurityFactory"></param>
        public static void StartListening(Uri address,
                                          IHostListenerFactory hostListenerFactory,
                                          object connectionHandler,
                                          ISecurityFactory serverSecurityFactory)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (ThreadLock.Lock(myListeners))
                    {
                        // Get all possible end points for the given hostname/address.
                        IEnumerable<IPEndPoint> anEndPoints = GetEndPoints(address);
                        foreach (IPEndPoint anEndPoint in anEndPoints)
                        {
                            // Try to get existing host listener for the endpoint.
                            HostListenerBase aHostListener = GetHostListener(anEndPoint);
                            if (aHostListener == null)
                            {
                                // The host listener does not exist so create it.
                                aHostListener = hostListenerFactory.CreateHostListener(anEndPoint, serverSecurityFactory);

                                // Register the path listener.
                                aHostListener.RegisterListener(address, connectionHandler);

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

                                // Register the path listener.
                                aHostListener.RegisterListener(address, connectionHandler);
                            }
                        }
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
                    using (ThreadLock.Lock(myListeners))
                    {
                        // Get all possible listening endpoints.
                        IEnumerable<IPEndPoint> anEndPoints = GetEndPoints(uri);
                        foreach (IPEndPoint anEndPoint in anEndPoints)
                        {
                            // Figure out if exist a host listener for the endpoint.
                            HostListenerBase aHostListener = GetHostListener(anEndPoint);
                            if (aHostListener != null)
                            {
                                // Unregister the path from the host listener.
                                aHostListener.UnregisterListener(uri);

                                // If there is no a path listener then nobody is interested in incoming messages
                                // and the TCP listening can be stopped.
                                if (aHostListener.ExistAnyListener() == false)
                                {
                                    myListeners.Remove(aHostListener);
                                }
                            }
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
                using (ThreadLock.Lock(myListeners))
                {
                    // Get all possible listening endpoints.
                    // Note: if URI contains hostname (instead of IP) then it returns false if none endpoints is listening.
                    IEnumerable<IPEndPoint> anEndPoints = GetEndPoints(uri);
                    foreach (IPEndPoint anEndPoint in anEndPoints)
                    {
                        // Figure out if exist a host listener for the endpoint.
                        HostListenerBase aHostListener = GetHostListener(anEndPoint);
                        if (aHostListener != null)
                        {
                            // Figure out if the path listener exists.
                            if (aHostListener.ExistListener(uri))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }
        }

        private static HostListenerBase GetHostListener(IPEndPoint endPoint)
        {
            using (EneterTrace.Entering())
            {
                HostListenerBase aHostListener = myListeners.FirstOrDefault(x => x.Address.Equals(endPoint));
                return aHostListener;
            }
        }

        private static IEnumerable<IPEndPoint> GetEndPoints(Uri address)
        {
            IPAddress[] anIpAddresses = Dns.GetHostAddresses(address.Host);
            IPEndPoint[] anEndPoints = new IPEndPoint[anIpAddresses.Length];
            for (int i = 0; i < anEndPoints.Length; ++i)
            {
                anEndPoints[i] = new IPEndPoint(anIpAddresses[i], address.Port);
            }

            return anEndPoints;
        }


        // List of IP address : port listeners. These listeners then maintain particular path listeners.
        private static List<HostListenerBase> myListeners = new List<HostListenerBase>();

        private static string TracedObject { get { return "HostListenerController "; } }
    }
}