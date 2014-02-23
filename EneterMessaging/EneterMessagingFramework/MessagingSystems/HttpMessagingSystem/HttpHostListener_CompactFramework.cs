/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
	/// <summary>
	/// The HTTP host listener listens to a particular IP address and port.
	/// It receives the HTTP request and forwards the request according to the path to the correct handler.
	/// E.g. if the client sends the request to  http://127.0.0.1/aaa/bbb/ then
	/// the HTTP host listene receives the request. It parses the HTTP header and finds the path /aaa/bbb/.
	/// Then it searches the handler and calls it to server the request.
	/// </summary>
    internal class HttpHostListener : HostListenerBase
    {
        public HttpHostListener(IPEndPoint address, ISecurityFactory securityFactory)
            : base(address, securityFactory)
        {
        }

        protected override void HandleConnection(TcpClient tcpClient)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Get the data stream.
                    // Note: If SSL then perform the authentication and provide the stream encoding/decoding data.
                    Stream aDataStream = SecurityFactory.CreateSecurityStreamAndAuthenticate(tcpClient.GetStream());

                    // Read Http header.
                    Match anHttpRequestRegex = HttpFormatter.DecodeHttpRequest(aDataStream);
                    if (!anHttpRequestRegex.Success)
                    {
                        EneterTrace.Warning(TracedObject + "failed to receive http request.");
                        byte[] aCloseConnectionResponse = HttpFormatter.EncodeError(400);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                        return;
                    }

                    // Get http header fields.
                    IDictionary<string, string> aHeaderFields = HttpFormatter.GetHttpHeaderFields(anHttpRequestRegex);

                    // Get the absolute path to identify the end-point.
                    string anIncomingPath = anHttpRequestRegex.Groups["path"].Value;
                    if (string.IsNullOrEmpty(anIncomingPath))
                    {
                    	EneterTrace.Warning(TracedObject + "failed to process HTTP request because the path is null or empty string.");
                        byte[] aCloseConnectionResponse = HttpFormatter.EncodeError(400);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                        return;
                    }
                    
                    // if the incoming path is the whole uri then extract the absolute path.
                    Uri anIncomingUri;
                    Uri.TryCreate(anIncomingPath, UriKind.Absolute, out anIncomingUri);
                    string anAbsolutePath = (anIncomingUri != null) ? anIncomingUri.AbsolutePath : anIncomingPath;

                    // Get handler for that path.
                    Uri aHandlerUri;
                    Action<HttpRequestContext> aPathHandler;
                    lock (myHandlers)
                    {
                        KeyValuePair<Uri, object> aPair = myHandlers.FirstOrDefault(x => x.Key.AbsolutePath == anAbsolutePath);
                        aPathHandler = aPair.Value as Action<HttpRequestContext>;
                        aHandlerUri = aPair.Key;
                    }

                    // If the listener does not exist.
                    if (aPathHandler == null)
                    {
                        EneterTrace.Warning(TracedObject + "does not listen to " + anIncomingPath);
                        byte[] aCloseConnectionResponse = HttpFormatter.EncodeError(404);
                        aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                        return;
                    }

                    // Create context for the received request message
                    string aRequestUriStr = aHandlerUri.Scheme + "://" + aHandlerUri.Host + ":" + Address.Port + anAbsolutePath + anHttpRequestRegex.Groups["query"].Value;
                    Uri aRequestUri = new Uri(aRequestUriStr);

                    string anHttpMethod = anHttpRequestRegex.Groups["method"].Value;
                    
                    // Read the content of the request message.
                    byte[] aRequestMessage = null;

                    // If the request message comes in chunks then read chunks.
                    if (aHeaderFields.Contains(new KeyValuePair<string, string>("Transfer-encoding", "chunked")))
                    {
                        using (MemoryStream aBuffer = new MemoryStream())
                        {
                            while (true)
                            {
                                byte[] aChunkData = HttpFormatter.DecodeChunk(aDataStream);
                                if (aChunkData != null && aChunkData.Length > 0)
                                {
                                    aBuffer.Write(aChunkData, 0, aChunkData.Length);
                                }
                                else
                                {
                                    // End of chunks reading.
                                    break;
                                }
                            }

                            aRequestMessage = aBuffer.ToArray();
                        }
                    }
                    else if (anHttpMethod == "POST")
                    {
                        // Get size of the message.
                        string aSizeStr;
                        aHeaderFields.TryGetValue("Content-Length", out aSizeStr);
                        if (aSizeStr == null)
                        {
                            EneterTrace.Warning(TracedObject + "failed to receive http request. The Content-Length is missing.");
                            byte[] aCloseConnectionResponse = HttpFormatter.EncodeError(400);
                            aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                            return;
                        }

                        int aSize;
                        try
                        {
                            aSize = int.Parse(aSizeStr);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to receive http request. The Content-Length was not a valid number.", err);
                            byte[] aCloseConnectionResponse = HttpFormatter.EncodeError(400);
                            aDataStream.Write(aCloseConnectionResponse, 0, aCloseConnectionResponse.Length);

                            return;
                        }

                        aRequestMessage = StreamUtil.ReadBytes(aDataStream, aSize);
                    }

                    // Http message from the client.
                    IPEndPoint aRemoteEndpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                    HttpRequestContext aClientContext = new HttpRequestContext(aRequestUri, anHttpMethod, aRemoteEndpoint, aRequestMessage, aDataStream);

                    try
                    {
                        aPathHandler(aClientContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }

                    // If the response was not sent then sent OK response.
                    if (!aClientContext.IsResponded)
                    {
                        aClientContext.Response(null);
                    }
                }
                catch (IOException err)
                {
                    EneterTrace.Warning(TracedObject + "detected closed connection.", err);
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