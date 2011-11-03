/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT && !MONO

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.IO.Pipes;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeInputChannel : IInputChannel
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public NamedPipeInputChannel(string channelId, int numberOfListeningInstancies, PipeSecurity pipeSecurity)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;

                // Parse the string format to get the server name and the pipe name.
                Uri aPath = null;
                Uri.TryCreate(channelId, UriKind.Absolute, out aPath);
                if (aPath != null)
                {
                    for (int i = 1; i < aPath.Segments.Length; ++i)
                    {
                        myPipeName += aPath.Segments[i].TrimEnd('/');
                    }
                }
                else
                {
                    string anError = TracedObject + ErrorHandler.InvalidUriAddress;
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                ChannelId = channelId;
                myNumberOfListeningInstances = numberOfListeningInstancies;
                myPipeSecurity = pipeSecurity;
            }
        }

        public string ChannelId { get; private set; }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Start the working thread for removing messages from the queue
                        myWorkingThread.RegisterMessageHandler(MessageHandler);

                        // Create instances listening to the pipe.
                        // Note: Instances represent the number of parallel listeners to the pipe.
                        //       All these threads just receive the message then put it to the queue and wait for another connected
                        //       client.
                        for (int i = 0; i < myNumberOfListeningInstances; ++i)
                        {
                            ListeningInstance aListeningInstance = new ListeningInstance(myPipeName, myNumberOfListeningInstances, myPipeSecurity, myWorkingThread);
                            myListeningInstances.Add(aListeningInstance);
                            aListeningInstance.StartListening();
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                        try
                        {
                            StopListening();
                        }
                        catch (Exception)
                        {
                            // We tried to clean after the failure.
                            // We can ignore this exception.
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
                lock (myListeningManipulatorLock)
                {
                    try
                    {
                        if (myListeningInstances.Count > 0)
                        {
                            // Set the flag about the stop to all listening instances.
                            foreach (ListeningInstance aListeningInstance in myListeningInstances)
                            {
                                aListeningInstance.SetStopListeningFlag();
                            }

                            // Stop all listening instances for this pipe.
                            ListeningInstance.StopListening(myPipeName, myListeningInstances.Count);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    myListeningInstances.Clear();
                    myWorkingThread.UnregisterMessageHandler();
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myListeningManipulatorLock)
                    {
                        return myListeningInstances.Count > 0;
                    }
                }
            }
        }

        /// <summary>
        /// The method is called from the working thread. When a message shall be processed.
        /// Messages comming from the pipe from diffrent threads are put to the queue where the working
        /// thread removes them one by one and notify the subscribers on the input channel.
        /// Therefore the channel notifies always in one thread.
        /// </summary>
        private void MessageHandler(object message)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new ChannelMessageEventArgs(ChannelId, message));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private List<ListeningInstance> myListeningInstances = new List<ListeningInstance>();

        private WorkingThread<object> myWorkingThread = new WorkingThread<object>();

        private int myNumberOfListeningInstances;

        private object myListeningManipulatorLock = new object();

        private string myPipeName;

        private PipeSecurity myPipeSecurity;


        private string TracedObject
        {
            get 
            {
                return "The named pipe input channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif