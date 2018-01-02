/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

#if WINDOWS_PHONE80 || WINDOWS_PHONE81
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketHostListener : HostListenerBase
    {
        public WebSocketHostListener(IPEndPoint address, ISecurityFactory securityFactory, bool reuseAddressFlag, int maxAmountOfConnections)
            : base(address, securityFactory, reuseAddressFlag, maxAmountOfConnections)
        {
        }


        // Handles TCP connection.
        // It parses the HTTP request of the websocket to get the requested path.
        // Then it searches the matching PathListeners and calls it to handle the connection.
        protected override void HandleConnection(TcpClient tcpClient)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Get the data stream.
                    // Note: If SSL then perform the authentication and provide the stream encoding/decoding data.
                    Stream aDataStream = SecurityFactory.CreateSecurityStreamAndAuthenticate(tcpClient.GetStream());

                    // Receive open websocket communication request.
                    Match anHttpOpenConnectionRegEx = WebSocketFormatter.DecodeOpenConnectionHttpRequest(aDataStream);
                    if (!anHttpOpenConnectionRegEx.Success)
                    {
                        EneterTrace.Warning(TracedObject + "failed to receive open websocket connection request. (incorrect http request)");
                        byte[] aCloseConnectionResponse = WebSocketFormatter.EncodeCloseFrame(null, 400);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);
                        
                        return;
                    }


                    // Get http header fields.
                    IDictionary<string, string> aHeaderFields = WebSocketFormatter.GetHttpHeaderFields(anHttpOpenConnectionRegEx);

                    string aSecurityKey;
                    aHeaderFields.TryGetValue("Sec-WebSocket-Key", out aSecurityKey);

                    // If some required header field is missing or has incorrect value.
                    if (!aHeaderFields.ContainsKey("Upgrade") ||
                        !aHeaderFields.ContainsKey("Connection") ||
                        string.IsNullOrEmpty(aSecurityKey))
                    {
                        EneterTrace.Warning(TracedObject + "failed to receive open websocket connection request. (missing or incorrect header field)");
                        byte[] aCloseConnectionResponse = WebSocketFormatter.EncodeCloseFrame(null, 400);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);
                        
                        return;
                    }

                    // Get the path to identify the end-point.
                    string anIncomingPath = anHttpOpenConnectionRegEx.Groups["path"].Value;
                    if (string.IsNullOrEmpty(anIncomingPath))
                    {
                        EneterTrace.Warning(TracedObject + "failed to process Websocket request because the path is null or empty string.");
                        byte[] aCloseConnectionResponse = WebSocketFormatter.EncodeCloseFrame(null, 400);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                        return;
                    }

                    // if the incoming path is the whole uri then extract the absolute path.
                    Uri anIncomingUri;
                    Uri.TryCreate(anIncomingPath, UriKind.Absolute, out anIncomingUri);
                    string anAbsolutePath = (anIncomingUri != null) ? anIncomingUri.AbsolutePath : anIncomingPath;

                    // Get handler for that path.
                    Uri aHandlerUri;
                    Action<IWebSocketClientContext> aPathHandler;
                    using (ThreadLock.Lock(myHandlers))
                    {
                        KeyValuePair<Uri, object> aPair = myHandlers.FirstOrDefault(x => x.Key.AbsolutePath == anAbsolutePath);
                        aPathHandler = aPair.Value as Action<IWebSocketClientContext>;
                        aHandlerUri = aPair.Key;
                    }

                    // If the listener does not exist.
                    if (aPathHandler == null)
                    {
                        EneterTrace.Warning(TracedObject + "does not listen to " + anIncomingPath);
                        byte[] aCloseConnectionResponse = WebSocketFormatter.EncodeCloseFrame(null, 404);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);
                        
                        return;
                    }

                    // Response that the connection is accepted.
                    byte[] anOpenConnectionResponse = WebSocketFormatter.EncodeOpenConnectionHttpResponse(aSecurityKey);
                    aDataStream.Write(anOpenConnectionResponse, 0, anOpenConnectionResponse.Length);


                    // Create the context for conecting client.
                    string aRequestUriStr = aHandlerUri.Scheme + "://" + aHandlerUri.Host + ":" + Address.Port + anAbsolutePath + anHttpOpenConnectionRegEx.Groups["query"].Value;
                    Uri aRequestUri = new Uri(aRequestUriStr);
                    WebSocketClientContext aClientContext = new WebSocketClientContext(aRequestUri, aHeaderFields, tcpClient, aDataStream);


                    // Call path handler in a another thread.
                    ThreadPool.QueueUserWorkItem(x =>
                        {
                            try
                            {
                                aPathHandler(aClientContext);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        });


                    // Start listening loop in the client context.
                    // The loop will read websocket messages from the underlying tcp connection.
                    // Note: User is responsible to call WebSocketClientContext.CloseConnection() to stop this loop
                    //       or the service must close the conneciton.
                    aClientContext.DoRequestListening();
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to process TCP connection.", err);
                }
            }
        }

        protected override string TracedObject
        {
            get { return GetType().Name + " "; }
        }
    }
}


#endif