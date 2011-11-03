/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT
#if !WINDOWS_PHONE

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.DataProcessing.Sequencing;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightInputChannel : IInputChannel, IDisposable
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public SilverlightInputChannel(string receiverId, ILocalSenderReceiverFactory localSenderReceiverFactory)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(receiverId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = receiverId;
                myLocalSenderReceiverFactory = localSenderReceiverFactory;

                FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
                myMultiInstanceFragmentFinalizer = aFragmentDataFactory.CreateMultiinstanceFragmentProcessor(aFragmentDataFactory.CreateSequenceFinalizer);
            }
        }

        #region Dispose/Finalize

        private bool isDisposed = false;

        /// <summary>
        /// Explicit dispose called by a user.
        /// </summary>
        public void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this); 
        }
        
        /// <summary>
        /// Method disposing managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing) 
        {
            if(!isDisposed)
            {
                if (disposing) 
                {
                    // Get rid of managed resources
                    if (myLocalMessageReceiver != null)
                    {
                        // Unregister message receiver.
                        myLocalMessageReceiver.MessageReceived -= OnEneterMessageReceived;
                        myLocalMessageReceiver.Dispose();
                        myLocalMessageReceiver = null;
                    }
                }
            }      
            
            // Get rid of unmanaged resources


            isDisposed = true;
        }
        
        /// <summary>
        /// Finalizer called by the garbage collector.
        /// </summary>
        ~SilverlightInputChannel()
        {
            Dispose(false);
        }

        #endregion

        public string ChannelId { get; private set; }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                if (IsListening)
                {
                    string anError = TracedObject + ErrorHandler.IsAlreadyListening;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    myLocalMessageReceiver = myLocalSenderReceiverFactory.CreateLocalMessageReceiver(ChannelId);
                    myLocalMessageReceiver.MessageReceived += OnEneterMessageReceived;
                    myLocalMessageReceiver.Listen();
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                    try
                    {
                        StopListening();
                    }
                    catch
                    {
                        // We tried to clean after the failure.
                        // We can ignore this exception.
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                if (IsListening)
                {
                    try
                    {
                        // Unregister message receiver.
                        myLocalMessageReceiver.MessageReceived -= OnEneterMessageReceived;
                        myLocalMessageReceiver.Dispose();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }
                    finally
                    {
                        myLocalMessageReceiver = null;
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myLocalMessageReceiver != null;
                }
            }
        }

        /// <summary>
        /// The method processes the incomming message.
        /// When the message is received it recognizes if it is a simple message or a split message.
        /// The simple message is directly notified.
        /// The split message is stored in the collector and is notified when the whole message is collected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEneterMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If nobody is subscribed then nothing to do.
                if (MessageReceived != null)
                {
                    SilverlightFragmentMessage aFragmentMessage = null;
                    try
                    {
                        aFragmentMessage = mySerializer.Deserialize<SilverlightFragmentMessage>(e.Message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to deserialize received message.", err);
                        return;
                    }

                    IEnumerable<IFragment> aFragments = null;
                    try
                    {
                        // Process message in the multiinstance fragment finalizer.
                        // Note: It returns the sorted fragment when all fragments for the instance are collected.
                        //       If the fragment is not collected it returns null or empty fragment.
                        aFragments = myMultiInstanceFragmentFinalizer.ProcessFragment(aFragmentMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to process the sequence of incoming message fragments.", err);
                        return;
                    }

                    try
                    {
                        // If all fragments for the instance are collected.
                        if (aFragments.Count() > 0)
                        {
                            // Go via all fragments and get the serialized string of the originaly split message.
                            string aSerializedOriginalMsg = "";
                            foreach (IFragment aFragment in aFragments)
                            {
                                SilverlightFragmentMessage aSilverlightFragment = aFragment as SilverlightFragmentMessage;
                                if (aSilverlightFragment != null)
                                {
                                    aSerializedOriginalMsg += aSilverlightFragment.Message;
                                }
                            }

                            try
                            {
                                // Notify subscribers
                                MessageReceived(this, new ChannelMessageEventArgs(ChannelId, aSerializedOriginalMsg));
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageFailure, err);
                    }

                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        /// <summary>
        /// Sirvelight's message receiver.
        /// </summary>
        private ILocalMessageReceiver myLocalMessageReceiver;
        private ILocalSenderReceiverFactory myLocalSenderReceiverFactory;

        private IMultiInstanceFragmentProcessor myMultiInstanceFragmentFinalizer;

        private ISerializer mySerializer = new XmlStringSerializer();


        private string TracedObject
        {
            get
            {
                return "The silverlight input channel '" + ChannelId + "' ";
            }
        }
    }
}

#endif
#endif