/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Messaging;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightReceiver
    {
        public SilverlightReceiver(string receiverAddress)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(receiverAddress))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myReceiverAddress = receiverAddress;

                FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
                myMultiInstanceFragmentFinalizer = aFragmentDataFactory.CreateMultiinstanceFragmentProcessor(aFragmentDataFactory.CreateSequenceFinalizer);
            }
        }

        public void StartListening(Action<string> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageHandler = messageHandler;

                    myReceiver = new LocalMessageReceiver(myReceiverAddress);
                    myReceiver.MessageReceived += OnMessageReceived;
                    ToSilverlightThread(() => myReceiver.Listen());
                }
                catch
                {
                    StopListening();
                    throw;
                }

            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                if (myReceiver != null)
                {
                    myReceiver.MessageReceived -= OnMessageReceived;
                    ToSilverlightThread(() =>
                        {
                            myReceiver.Dispose();
                            myReceiver = null;
                        });
                }

                myMessageHandler = null;
            }
        }

        public bool IsListening
        {
            get { return myReceiver != null; }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (EneterTrace.Entering())
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

                IEnumerable<Fragment> aFragments = null;
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
                    if (aFragments != null && aFragments.Any())
                    {
                        // Go via all fragments and get the serialized string of the originaly split message.
                        StringBuilder aSerializedOriginalMsg = new StringBuilder();
                        foreach (Fragment aFragment in aFragments)
                        {
                            SilverlightFragmentMessage aSilverlightFragment = aFragment as SilverlightFragmentMessage;
                            if (aSilverlightFragment != null)
                            {
                                aSerializedOriginalMsg.Append(aSilverlightFragment.Message);
                            }
                        }

                        try
                        {
                            if (myMessageHandler != null)
                            {
                                string aMessage = aSerializedOriginalMsg.ToString();
                                myMessageHandler(aMessage);
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToReceiveMessage, err);
                }
            }
        }

        private void ToSilverlightThread(Action a)
        {
            if (Deployment.Current.Dispatcher.CheckAccess())
            {
                a();
            }
            else
            {
                // Call this method again from the right Silverlight thread
                Deployment.Current.Dispatcher.BeginInvoke(a);
            }
        }


        private Action<string> myMessageHandler;
        private string myReceiverAddress;
        private LocalMessageReceiver myReceiver;
        private ISerializer mySerializer = new XmlStringSerializer();
        private IMultiInstanceFragmentProcessor myMultiInstanceFragmentFinalizer;


        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}

#endif