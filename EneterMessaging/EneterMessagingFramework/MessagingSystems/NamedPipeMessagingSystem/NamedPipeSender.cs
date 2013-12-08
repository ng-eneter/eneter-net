/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeSender : ISender, IDisposable
    {
        public NamedPipeSender(string uriPipeName, int timeOut)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Extract server name and pipe name from Uri.
                    Uri aUri = null;
                    try
                    {
                        aUri = new Uri(uriPipeName, UriKind.Absolute);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(uriPipeName + ErrorHandler.InvalidUriAddress, err);
                        throw;
                    }
                    string aServerName = aUri.Host;
                    string aPipeName = "";
                    for (int i = 1; i < aUri.Segments.Length; ++i)
                    {
                        aPipeName += aUri.Segments[i].TrimEnd('/');
                    }

                    myClientStream = new NamedPipeClientStream(aServerName, aPipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                    myClientStream.Connect(timeOut);

                    myTimeout = timeOut;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed in constructor.", err);
                    Dispose();
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (myClientStream != null)
            {
                AutoResetEvent anAllDataRead = new AutoResetEvent(false);
                ThreadPool.QueueUserWorkItem(x =>
                    {
                        try
                        {
                            myClientStream.WaitForPipeDrain();
                        }
                        catch
                        {
                            // This exception occurs when the dispose is called after the timeout.
                        }

                        anAllDataRead.Set();
                    });

                if (!anAllDataRead.WaitOne(1000))
                {
                    EneterTrace.Warning(TracedObject + "failed to wait until named pipe is completely read. It will be disposed.");
                }

                myClientStream.Close();
                myClientStream.Dispose();
                myClientStream = null;
            }
        }

        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    byte[] aMessage = (byte[])message;
                    myClientStream.Write(aMessage, 0, aMessage.Length);
                    //myClientStream.Flush();
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException();
        }


        private NamedPipeClientStream myClientStream;
        private int myTimeout;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif