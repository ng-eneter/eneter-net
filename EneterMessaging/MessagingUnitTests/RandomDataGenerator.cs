using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests
{
    internal static class RandomDataGenerator
    {
        public static string GetString(int length)
        {
            Random aRnd = new Random();
            StringBuilder aStringBuilder = new StringBuilder();

            for (int i = 0; i < length; ++i)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * aRnd.NextDouble() + 65)));
                aStringBuilder.Append(ch);
            }

            return aStringBuilder.ToString();
        }

        public static void GetBytes(byte[] data)
        {
            Random aRnd = new Random();
            aRnd.NextBytes(data);
        }
    }
}
