using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests
{
    internal static class RandomPortGenerator
    {
        public static string Generate()
        {
            Random aRnd = new Random();
            int aPort = aRnd.Next(7000, 8000);
            return aPort.ToString();
        }
    }
}
