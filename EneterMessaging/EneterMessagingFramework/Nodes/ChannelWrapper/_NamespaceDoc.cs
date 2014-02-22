/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Reducing communication via multiple channels into one channel.
    /// </summary>
    /// <remarks>
    /// The channel wrapper and unwrapper are components allowing to send/receive different types of messages via one channel.
    /// E.g. to send and receive multiple types of request messages via one IP address and port.
    /// <example>
    /// Simple service using the channel unwrapper to receive all request messages via one IP address and port.
    /// <code>
    /// namespace ServerCalculator2
    /// {
    ///     // Input data for calculator requests
    ///     public class CalculatorInputData
    ///     {
    ///         public double Number1 { get; set; }
    ///         public double Number2 { get; set; }
    ///     }
    /// 
    ///     // Output result from the calculator
    ///     public class CalculatorOutputData
    ///     {
    ///         public double Result { get; set; }
    ///     }
    /// 
    ///     internal class Calculator
    ///     {
    ///         public Calculator()
    ///         {
    ///             // Internal messaging used for messaging between channel unwrapper
    ///             // and typed message receivers.
    ///             // We want that requests do not block each other. So every request will be processed in its own thread.
    ///             IMessagingSystemFactory anInternalMessaging = new ThreadPoolMessagingSystemFactory();
    /// 
    ///             // All messages are received via one channel. So we must provide "unwrapper" forwarding incoming messages
    ///             // to correct receivers.
    ///             IChannelWrapperFactory aChannelWrapperFactory = new ChannelWrapperFactory();
    ///             myDuplexChannelUnwrapper = aChannelWrapperFactory.CreateDuplexChannelUnwrapper(anInternalMessaging);
    /// 
    ///             // To connect receivers and the unwrapper with duplex channels we can use the following helper class.
    ///             IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
    ///             IConnectionProvider aConnectionProvider = aConnectionProviderFactory.CreateConnectionProvider(anInternalMessaging);
    /// 
    ///             // Factory to create message receivers.
    ///             IDuplexTypedMessagesFactory aMessageReceiverFactory = new DuplexTypedMessagesFactory();
    ///             
    ///             // Create receiver to sum two numbers.
    ///             mySumReceiver = aMessageReceiverFactory.CreateDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             mySumReceiver.MessageReceived += SumCmd; // attach method handling the request
    ///             aConnectionProvider.Attach(mySumReceiver, "Sum"); // attach the input channel to get messages from unwrapper
    /// 
    ///             // Receiver to subtract two numbers.
    ///             mySubtractReceiver = aMessageReceiverFactory.CreateDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             mySubtractReceiver.MessageReceived += SubCmd; // attach method handling the request
    ///             aConnectionProvider.Attach(mySubtractReceiver, "Sub"); // attach the input channel to get messages from unwrapper
    /// 
    ///             // Receiver for multiply two numbers.
    ///             myMultiplyReceiver = aMessageReceiverFactory.CreateDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             myMultiplyReceiver.MessageReceived += MulCmd; // attach method handling the request
    ///             aConnectionProvider.Attach(myMultiplyReceiver, "Mul"); // attach the input channel to get messages from unwrapper
    /// 
    ///             // Receiver for divide two numbers.
    ///             myDivideReceiver = aMessageReceiverFactory.CreateDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             myDivideReceiver.MessageReceived += DivCmd; // attach method handling the request
    ///             aConnectionProvider.Attach(myDivideReceiver, "Div"); // attach the input channel to get messages from unwrapper
    ///         }
    /// 
    /// 
    ///         public void Start()
    ///         {
    ///             // We use TCP based messaging.
    ///             IMessagingSystemFactory aServiceMessagingSystem = new TcpMessagingSystemFactory();
    ///             IDuplexInputChannel anInputChannel = aServiceMessagingSystem.CreateDuplexInputChannel("tcp://127.0.0.1:8091/");
    /// 
    ///             // Attach the input channel to the unwrapper and start to listening.
    ///             myDuplexChannelUnwrapper.AttachDuplexInputChannel(anInputChannel);
    ///         }
    /// 
    ///         public void Stop()
    ///         {
    ///             // Detach the input channel from the unwrapper and stop listening.
    ///             // Note: It releases listening threads.
    ///             myDuplexChannelUnwrapper.DetachDuplexInputChannel();
    ///         }
    /// 
    ///         // It is called when a request to sum two numbers was received.
    ///         private void SumCmd(object sender, TypedRequestReceivedEventArgs&lt;CalculatorInputData&gt; e)
    ///         {
    ///             // Get input data.
    ///             CalculatorInputData anInputData = e.RequestMessage;
    /// 
    ///             // Calculate output result.
    ///             CalculatorOutputData aReturn = new CalculatorOutputData();
    ///             aReturn.Result = anInputData.Number1 + anInputData.Number2;
    /// 
    ///             Console.WriteLine("{0} + {1} = {2}", anInputData.Number1, anInputData.Number2, aReturn.Result);
    /// 
    ///             // Response result to the client.
    ///             mySumReceiver.SendResponseMessage(e.ResponseReceiverId, aReturn);
    ///         }
    /// 
    ///         // It is called when a request to subtract two numbers was received.
    ///         private void SubCmd(object sender, TypedRequestReceivedEventArgs&lt;CalculatorInputData&gt; e)
    ///         {
    ///             // Get input data.
    ///             CalculatorInputData anInputData = e.RequestMessage;
    /// 
    ///             // Calculate output result.
    ///             CalculatorOutputData aReturn = new CalculatorOutputData();
    ///             aReturn.Result = anInputData.Number1 - anInputData.Number2;
    /// 
    ///             Console.WriteLine("{0} - {1} = {2}", anInputData.Number1, anInputData.Number2, aReturn.Result);
    /// 
    ///             // Response result to the client.
    ///             mySubtractReceiver.SendResponseMessage(e.ResponseReceiverId, aReturn);
    ///         }
    ///         
    /// 
    ///         // It is called when a request to multiply two numbers was received.
    ///         private void MulCmd(object sender, TypedRequestReceivedEventArgs&lt;CalculatorInputData&gt; e)
    ///         {
    ///             // Get input data.
    ///             CalculatorInputData anInputData = e.RequestMessage;
    /// 
    ///             // Calculate output result.
    ///             CalculatorOutputData aReturn = new CalculatorOutputData();
    ///             aReturn.Result = anInputData.Number1 * anInputData.Number2;
    /// 
    ///             Console.WriteLine("{0} x {1} = {2}", anInputData.Number1, anInputData.Number2, aReturn.Result);
    /// 
    ///             // Response result to the client.
    ///             myMultiplyReceiver.SendResponseMessage(e.ResponseReceiverId, aReturn);
    ///         }
    /// 
    ///         // It is called when a request to divide two numbers was received.
    ///         private void DivCmd(object sender, TypedRequestReceivedEventArgs&lt;CalculatorInputData&gt; e)
    ///         {
    ///             // Get input data.
    ///             CalculatorInputData anInputData = e.RequestMessage;
    /// 
    ///             // Calculate output result.
    ///             CalculatorOutputData aReturn = new CalculatorOutputData();
    ///             aReturn.Result = anInputData.Number1 / anInputData.Number2;
    /// 
    ///             Console.WriteLine("{0} / {1} = {2}", anInputData.Number1, anInputData.Number2, aReturn.Result);
    /// 
    ///             // Response result to the client.
    ///             myDivideReceiver.SendResponseMessage(e.ResponseReceiverId, aReturn);
    ///         }
    ///         
    /// 
    ///         // Unwrapps messages from the input channel and forwards them
    ///         // to corresponding output channels.
    ///         private IDuplexChannelUnwrapper myDuplexChannelUnwrapper;
    /// 
    ///         // Paticular services listening to requests which will be forwarded from
    ///         // the channel unwrapper.
    ///         private IDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt; mySumReceiver;
    ///         private IDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt; mySubtractReceiver;
    ///         private IDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt; myMultiplyReceiver;
    ///         private IDuplexTypedMessageReceiver&lt;CalculatorOutputData, CalculatorInputData&gt; myDivideReceiver;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Client using channel wrapper to send all request messages one IP address and port.
    /// <code>
    /// namespace CalculatorClient2
    /// {
    ///     public partial class Form1 : Form
    ///     {
    ///         // Input data for calculator requests
    ///         public class CalculatorInputData
    ///         {
    ///             public double Number1 { get; set; }
    ///             public double Number2 { get; set; }
    ///         }
    /// 
    ///         // Output result from the calculator
    ///         public class CalculatorOutputData
    ///         {
    ///             public double Result { get; set; }
    ///         }
    /// 
    ///         public Form1()
    ///         {
    ///             InitializeComponent();
    /// 
    ///             // Internal messaging between message senders and channel wrapper.
    ///             IMessagingSystemFactory anInternalMessaging = new SynchronousMessagingSystemFactory();
    /// 
    ///             // The service receives messages via one channel (i.e. it listens on one address).
    ///             // The incoming messages are unwrapped on the server side.
    ///             // Therefore the client must use wrapper to send messages via one channel.
    ///             IChannelWrapperFactory aChannelWrapperFactory = new ChannelWrapperFactory();
    ///             myDuplexChannelWrapper = aChannelWrapperFactory.CreateDuplexChannelWrapper();
    /// 
    /// 
    ///             // To connect message senders and the wrapper with duplex channels we can use the following helper class.
    ///             IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
    ///             IConnectionProvider aConnectionProvider = aConnectionProviderFactory.CreateConnectionProvider(anInternalMessaging);
    /// 
    ///             
    ///             // Factory to create message senders.
    ///             // Sent messages will be serialized in Xml.
    ///             IDuplexTypedMessagesFactory aCommandsFactory = new DuplexTypedMessagesFactory();
    /// 
    ///             // Sender to sum two numbers.
    ///             mySumSender = aCommandsFactory.CreateDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             mySumSender.ResponseReceived += OnResultResponse;
    ///             aConnectionProvider.Connect(myDuplexChannelWrapper, mySumSender, "Sum");
    /// 
    ///             // Sender to subtract two numbers.
    ///             mySubSender = aCommandsFactory.CreateDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             mySubSender.ResponseReceived += OnResultResponse;
    ///             aConnectionProvider.Connect(myDuplexChannelWrapper, mySubSender, "Sub");
    /// 
    ///             // Sender to multiply two numbers.
    ///             myMulSender = aCommandsFactory.CreateDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             myMulSender.ResponseReceived += OnResultResponse;
    ///             aConnectionProvider.Connect(myDuplexChannelWrapper, myMulSender, "Mul");
    /// 
    ///             // Sender to divide two numbers.
    ///             myDivSender = aCommandsFactory.CreateDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt;();
    ///             myDivSender.ResponseReceived += OnResultResponse;
    ///             aConnectionProvider.Connect(myDuplexChannelWrapper, myDivSender, "Div");
    /// 
    ///             // We use Tcp for the communication.
    ///             IMessagingSystemFactory aTcpMessagingSystem = new TcpMessagingSystemFactory();
    /// 
    ///             // Create output channel to send requests to the service.
    ///             IDuplexOutputChannel anOutputChannel = aTcpMessagingSystem.CreateDuplexOutputChannel("tcp://127.0.0.1:8091/");
    /// 
    ///             // Attach the output channel to the wrapper - so that we are able to send messages
    ///             // and receive response messages.
    ///             // Note: The service has the coresponding unwrapper.
    ///             myDuplexChannelWrapper.AttachDuplexOutputChannel(anOutputChannel);
    ///         }
    /// 
    ///         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///         {
    ///             // Stop listening by detaching the input channel.
    ///             myDuplexChannelWrapper.DetachDuplexInputChannel();
    ///         }
    /// 
    /// 
    ///         private void OnResultResponse(object sender, TypedResponseReceivedEventArgs&lt;CalculatorOutputData&gt; e)
    ///         {
    ///             // If everything is ok then display the result.
    ///             if (e.ReceivingError == null)
    ///             {
    ///                 // The response does not come in main UI thread.
    ///                 // Therefore we must transfer it to the main UI thread.
    ///                 InvokeInUIThread(() =&gt; ResultLabel.Text = e.ResponseMessage.Result.ToString() );
    ///             }
    ///         }
    /// 
    ///         private void CalculateButton_Click(object sender, EventArgs e)
    ///         {
    ///             SendRequestMessage(mySumSender);
    ///         }
    ///          
    ///         private void SubtractButton_Click(object sender, EventArgs e)
    ///         {
    ///             SendRequestMessage(mySubSender);
    ///         }
    /// 
    ///         private void MultiplyButton_Click(object sender, EventArgs e)
    ///         {
    ///             SendRequestMessage(myMulSender);
    ///         }
    /// 
    ///         private void DivideButton_Click(object sender, EventArgs e)
    ///         {
    ///             SendRequestMessage(myDivSender);
    ///         }
    /// 
    ///         private void SendRequestMessage(IDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt; sender)
    ///         {
    ///             // Prepare input data for the calculator.
    ///             CalculatorInputData anInputForCalculator = new CalculatorInputData();
    ///             anInputForCalculator.Number1 = double.Parse(Number1TextBox.Text);
    ///             anInputForCalculator.Number2 = double.Parse(Number2TextBox.Text);
    /// 
    ///             // Send the request message.
    ///             sender.SendRequestMessage(anInputForCalculator);
    ///         }
    /// 
    ///         // Helper method to invoke UI always in the correct thread.
    ///         private void InvokeInUIThread(Action action)
    ///         {
    ///             if (InvokeRequired)
    ///             {
    ///                 Invoke(action);
    ///             }
    ///             else
    ///             {
    ///                 action.Invoke();
    ///             }
    ///         }
    /// 
    ///         // Wraps requests into one output channel.
    ///         // The service side listens to one address and uses unwrapper to unwrap
    ///         // messages and send them to correct receivers.
    ///         private IDuplexChannelWrapper myDuplexChannelWrapper;
    /// 
    ///         // Message senders.
    ///         private IDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt; mySumSender;
    ///         private IDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt; mySubSender;
    ///         private IDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt; myMulSender;
    ///         private IDuplexTypedMessageSender&lt;CalculatorOutputData, CalculatorInputData&gt; myDivSender;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
