/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System.Windows;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Helper class used in Silverlight environment to get the valid Uri for the hosting server.
    /// </summary>
    public static class ServerUri
    {
        /// <summary>
        /// Returns the Uri string for the given path on the hosting server.
        /// </summary>
        /// <param name="pathToHandlerInAsp">path to the message handler (ashx file)</param>
        /// <returns></returns>
        public static string GetServerUri(string pathToHandlerInAsp)
        {
            using (EneterTrace.Entering())
            {
                string aScheme = Application.Current.Host.Source.Scheme;
                string aPort = Application.Current.Host.Source.Port.ToString();
                string aHost = Application.Current.Host.Source.Host;
                string aUri = aScheme + "://" + aHost + ":" + aPort + pathToHandlerInAsp;

                return aUri;
            }
        }
    }
}

#endif
