using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class TopicSubscriberTest
    {
        [TestMethod]
        public void Test_BasicSubscribeUnsubscribe_And_TopicAccessors()
        {
            var subscriber = new MockTopicSubscriber();

            Assert.IsTrue(subscriber.IsEmptyAccessor());
            subscriber.Subscribe("test", m => { });
            Assert.IsFalse(subscriber.IsEmptyAccessor());

            IEnumerable<string> topics = subscriber.GetTopicsAccessor();
            Assert.AreEqual("test", topics.First());
            Assert.AreEqual("test", topics.Last());

            subscriber.Unsubscribe("test");
            Assert.IsTrue(subscriber.IsEmptyAccessor());
            Assert.AreEqual(0, subscriber.GetTopicsAccessor().Count());
        }

        [TestMethod]
        public void Test_SubscribeOverloads()
        {
            var subscriber = new MockTopicSubscriber();

            subscriber.Subscribe("test", m => Assert.AreEqual(111, m[0]));
            subscriber.InvokeSubscriptionAccessor("test", new byte[] {111});
            subscriber.Unsubscribe("test");
            subscriber.InvokeSubscriptionAccessor("test", new byte[] {222});

            subscriber.Subscribe("test1", m => Assert.AreEqual(111, m[0]));
            subscriber.Subscribe("test2", m => Assert.AreEqual(222, m[0]));
            subscriber.InvokeSubscriptionAccessor("test1", new byte[] {111});
            subscriber.InvokeSubscriptionAccessor("test2", new byte[] {222});


            subscriber.Subscribe(m => Assert.AreEqual(111, m.Data[0]), m => new MockMessage {Data = m});
            subscriber.InvokeSubscriptionAccessor(typeof (MockMessage).FullName, new byte[] {111});

            subscriber.Subscribe(new TopicSubscription<MockMessage>
            {
                MessageHandler = m => Assert.AreEqual(222, m.Data[0]),
                Deserializer = m => new MockMessage {Data = m}
            });
            subscriber.InvokeSubscriptionAccessor(typeof (MockMessage).FullName, new byte[] {222});

            subscriber.Subscribe(new TopicSubscription<MockMessage>
            {
                Topic = string.Empty,
                MessageHandler = m => Assert.AreEqual(123, m.Data[0]),
                Deserializer = m => new MockMessage {Data = m}
            });
            subscriber.InvokeSubscriptionAccessor(string.Empty, new byte[] {123});
        }


        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Test_SubscribeException()
        {
            var subscriber = new MockTopicSubscriber();
            subscriber.Subscribe<MockMessage>(null);
        }
    }
}