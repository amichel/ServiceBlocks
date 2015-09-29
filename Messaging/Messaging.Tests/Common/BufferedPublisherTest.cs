using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class BufferedPublisherTest
    {
        [TestMethod]
        public void Test_ConsumerAction_And_Start()
        {
            using (var countDown = new CountdownEvent(1))
            {
                using (var publisher = new MockBufferedPublisher(() => { countDown.Signal(); }))
                {
                    publisher.Start();
                    Assert.IsTrue(countDown.Wait(100));
                }
            }
        }

        [TestMethod]
        public void Test_Stop()
        {
            using (var countDown = new CountdownEvent(2))
            {
                using (var publisher = new MockBufferedPublisher(() => { countDown.Signal(); }))
                {
                    publisher.Start();
                    publisher.Stop(100);
                    publisher.Start();
                    Assert.IsTrue(countDown.Wait(500));
                }
            }
        }

        [TestMethod]
        public void Test_Publish()
        {
            using (var publisher = new MockBufferedPublisher())
            {
                Assert.AreEqual(0, publisher.Count);
                publisher.Publish(111, x => new[] {Convert.ToByte(x)});
                Assert.AreEqual(1, publisher.Count);
                publisher.Publish("test", 111, x => new[] {Convert.ToByte(x)});
                Assert.AreEqual(2, publisher.Count);
                publisher.Publish("test", new byte[] {111});
                Assert.AreEqual(3, publisher.Count);
            }
        }
    }
}