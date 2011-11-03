using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Streaming;
using System.IO;
using System.Threading;

namespace Eneter.MessagingUnitTests.DataProcessing.Streaming
{
    [TestFixture]
    public class Test_DynamicStream
    {
        [Test]
        public void WriteRead()
        {
            DynamicStream aDynamicStream = new DynamicStream();

            BinaryWriter aBinaryWriter = new BinaryWriter(aDynamicStream);
            aBinaryWriter.Write("Hello");
            aBinaryWriter.Write((int)123);

            BinaryReader aBinaryReader = new BinaryReader(aDynamicStream);
            string aString = aBinaryReader.ReadString();
            int aNumber = aBinaryReader.ReadInt32();

            Assert.AreEqual("Hello", aString);
            Assert.AreEqual(123, aNumber);
        }

        [Test]
        public void WriteReadMultithread()
        {
            DynamicStream aDynamicStream = new DynamicStream();

            string aString = "";
            int aNumber = 0;
            Action aReading = () =>
                {
                    BinaryReader aBinaryReader = new BinaryReader(aDynamicStream);
                    aString = aBinaryReader.ReadString();
                    aNumber = aBinaryReader.ReadInt32();
                };

            Action aWriting = () =>
                {
                    Thread.Sleep(100);

                    BinaryWriter aBinaryWriter = new BinaryWriter(aDynamicStream);
                    aBinaryWriter.Write("Hello");

                    Thread.Sleep(300);
                    
                    aBinaryWriter.Write((int)123);
                };


            // Invoke "slow" writing in another thread
            aWriting.BeginInvoke(null, null);

            // Invoke "fast" reading in this thread
            // This thread should be blocked until all messages are read.
            aReading.Invoke();
            

            Assert.AreEqual("Hello", aString);
            Assert.AreEqual(123, aNumber);
        }

        [Test]
        public void WriteFragmentsReadWholeMultithread()
        {
            DynamicStream aDynamicStream = new DynamicStream();

            string aString = "";
            Action aReading = () =>
            {
                BinaryReader aBinaryReader = new BinaryReader(aDynamicStream);
                aString = aBinaryReader.ReadString();
            };

            Action aWriting = () =>
            {
                Thread.Sleep(100);

                // Convert the string to bytes.
                MemoryStream aMemoryStream = new MemoryStream();
                BinaryWriter aBinaryWriter = new BinaryWriter(aMemoryStream);
                aBinaryWriter.Write("Hello_12345678901234567890_1234567890");

                byte[] aData = aMemoryStream.ToArray();

                // Put it byte by byte to the tested stream.
                foreach (byte b in aData)
                {
                    aDynamicStream.WriteByte(b);
                }
               
            };


            // Invoke "slow" writing in another thread
            aWriting.BeginInvoke(null, null);

            // Invoke "fast" reading in this thread
            // This thread should be blocked until all messages are read.
            aReading.Invoke();

            Assert.AreEqual("Hello_12345678901234567890_1234567890", aString);
        }

        [Test]
        public void UnblockReadingByClosing()
        {
            DynamicStream aDynamicStream = new DynamicStream();

            Action aClosing = () =>
            {
                Thread.Sleep(500);
                aDynamicStream.Close();
            };


            // Close the stream in another thread in 500 ms.
            aClosing.BeginInvoke(null, null);

            // Start to read in this thread. The reading will be blocked until the stream is closed.
            int aReadSize = aDynamicStream.ReadByte();

            Assert.AreEqual(-1, aReadSize);
        }

        [Test]
        public void OpenClose()
        {
            DynamicStream aDynamicStream = new DynamicStream();
            aDynamicStream.Close();
        }
    }
}
