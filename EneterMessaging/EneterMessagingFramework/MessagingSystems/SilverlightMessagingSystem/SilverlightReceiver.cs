

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Eneter.Messaging.Diagnostic;
using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing;
using Eneter.Messaging.DataProcessing.Serializing;
using System.Windows.Messaging;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

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

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (IsListening)
                {
                    throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyListening);
                }

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
                        string aSerializedOriginalMsg = "";
                        foreach (Fragment aFragment in aFragments)
                        {
                            SilverlightFragmentMessage aSilverlightFragment = aFragment as SilverlightFragmentMessage;
                            if (aSilverlightFragment != null)
                            {
                                aSerializedOriginalMsg += aSilverlightFragment.Message;
                            }
                        }

                        try
                        {
                            MessageContext aContext = new MessageContext(aSerializedOriginalMsg, "", null);
                            if (myMessageHandler != null)
                            {
                                myMessageHandler(aContext);
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
                    EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageFailure, err);
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


        private Func<MessageContext, bool> myMessageHandler;
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