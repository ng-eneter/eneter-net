


using System;
using System.Net;
using System.Net.Sockets;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal static class IPAddressExt
    {
        public static IPAddress ParseMulticastGroup(string multicastGroup)
        {
            IPAddress aMulticastIpAddress = IPAddress.Parse(multicastGroup);

            if (aMulticastIpAddress.IsIPv6Multicast)
            {
                return aMulticastIpAddress;
            }

            if (aMulticastIpAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] aBytes = aMulticastIpAddress.GetAddressBytes();
                if (aBytes[0] >= 224 && aBytes[0] <= 239)
                {
                    return aMulticastIpAddress;
                }
            }

            throw new InvalidOperationException("'" + multicastGroup + "' is not a multicast IP address.");
        }
    }
}