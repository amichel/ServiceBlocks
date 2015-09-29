using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class BufferedSubscriberTest
    {
        [TestMethod]
        public void Test_Producer_Action_And_Start()
        {
            using (var countDown = new CountdownEvent(1))
            {
                using (var subscriber = new MockBufferedSubscriber(() => { countDown.Signal(); }, () => { }, () => { }))
                {
                    subscriber.Start();
                    Assert.IsTrue(countDown.Wait(100));
                }
            }
        }

        [TestMethod]
        public void Test_Stop()
        {
            using (var countDown = new CountdownEvent(2))
            {
                using (var subscriber = new MockBufferedSubscriber(() => { countDown.Signal(); }, () => { }, () => { }))
                {
                    subscriber.Start();
                    subscriber.Stop(100);
                    subscriber.Start();
                    Assert.IsTrue(countDown.Wait(5000));
                }
            }
        }

        [TestMethod]
        public void Test_ConsumeMessage()
        {
            using (var countDown = new CountdownEvent(2))
            {
                using (var subscriber = new MockBufferedSubscriber(() => { }, () => { countDown.Signal(); }, () => { }))
                {
                    subscriber.Start();
                    subscriber.Publish(new MockMessage());
                    subscriber.Publish(new MockMessage());
                    Assert.IsTrue(countDown.Wait(500));
                }
            }
        }

        [TestMethod]
        public void Test_ConsumeMessage_WithError()
        {
            using (var countDown = new CountdownEvent(2))
            {
                using (
                    var subscriber = new MockBufferedSubscriber(() => { }, () => { throw new Exception(); },
                        () => { countDown.Signal(); }))
                {
                    subscriber.Start();
                    subscriber.Publish(new MockMessage());
                    subscriber.Publish(new MockMessage());
                    Assert.IsTrue(countDown.Wait(500));
                }
            }
        }
    }
}