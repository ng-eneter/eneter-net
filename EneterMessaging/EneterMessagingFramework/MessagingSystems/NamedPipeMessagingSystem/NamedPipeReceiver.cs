/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeReceiver
    {
        public NamedPipeReceiver(string pipeUriAddress, int numberOfPipeInstances, int responseConnectionTimeout, PipeSecurity pipeSecurity)
        {
            using (EneterTrace.Entering())
            {
                // Parse the string format to get the server name and the pipe name.
                string aPipeName = "";
                Uri aPath = null;
                Uri.TryCreate(pipeUriAddress, UriKind.Absolute, out aPath);
                if (aPath != null)
                {
                    for (int i = 1; i < aPath.Segments.Length; ++i)
                    {
                        aPipeName += aPath.Segments[i].TrimEnd('/');
                    }
                }
                else
                {
                    string anError = TracedObject + ErrorHandler.InvalidUriAddress;
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                myPipeName = aPipeName;
                myNumberOfInstances = numberOfPipeInstances;
                myPipeSecurity = pipeSecurity;
                myResponseConnectionTimeout = responseConnectionTimeout;
            }
        }

        public void StartListening(Action<Stream> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        for (int i = 0; i < myNumberOfInstances; ++i)
                        {
                            ListeningInstance aListeningInstance = new ListeningInstance(myPipeName, myNumberOfInstances, myPipeSecurity);
                            myPipeServerListeners.Add(aListeningInstance);

                            aListeningInstance.StartListening(messageHandler);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

                        try
                        {
                            // Clear after failed start
                            StopListening();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    try
                    {
                        ListeningInstance.StopListening(myPipeName, myPipeServerListeners);
                    }
                    catch
                    {
                    }

                    myPipeServerListeners.Clear();
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    return myPipeServerListeners.Any();
                }
            }
        }


        private int myNumberOfInstances;
        private string myPipeName;
        private PipeSecurity myPipeSecurity;
        private int myResponseConnectionTimeout;

        private object myListeningManipulatorLock = new object();
        private List<ListeningInstance> myPipeServerListeners = new List<ListeningInstance>();
        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif