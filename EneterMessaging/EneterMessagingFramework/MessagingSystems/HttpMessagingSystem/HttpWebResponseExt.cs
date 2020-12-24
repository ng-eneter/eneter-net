

using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using System.IO;
using System.Net;
using System.Text;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Extension methods for HttpWebResponse.
    /// </summary>
    public static class HttpWebResponseExt
    {
        /// <summary>
        /// Returns the whole response message in string.
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public static string GetResponseMessageStr(this HttpWebResponse httpResponse)
        {
            using (EneterTrace.Entering())
            {
                byte[] aBytes = httpResponse.GetResponseMessage();
                string aResult = Encoding.UTF8.GetString(aBytes);
                return aResult;
            }
        }

        /// <summary>
        /// Returns the whole response message in bytes.
        /// </summary>
        /// <param name="httpResponse">A response received from the HTTP server.</param>
        /// <returns>The response message in bytes.</returns>
        public static byte[] GetResponseMessage(this HttpWebResponse httpResponse)
        {
            using (EneterTrace.Entering())
            {
                byte[] aResult = null;

                Stream aWebResponseStream = httpResponse.GetResponseStream();
                if (aWebResponseStream != null)
                {
                    aResult = StreamUtil.ReadToEnd(httpResponse.GetResponseStream());
                }

                return aResult;
            }
        }
    }
}
