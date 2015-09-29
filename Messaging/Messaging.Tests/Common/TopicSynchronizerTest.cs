using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class TopicSynchronizerTest
    {
        [TestMethod]
        public void Test_Snapshot_Behind_Stream_EmptyCache()
        {
            var cache = new DefaultLastValueCache<int, MockMessage>();
            var subscriber = new MockTopicSubscriber();
            var snapshotClient = new MockSnapshotClient
            {
                Data = new[]
                {
                    new MockMessage {Data = new byte[] {1, 11}},
                    new MockMessage {Data = new byte[] {2, 22}},
                    new MockMessage {Data = new byte[] {3, 33}}
                }
            };

            var subscription = new TopicSubscription<MockMessage>
            {
                Topic = "",
                Deserializer = m => new MockMessage {Data = m},
                MessageHandler = m => Debug.WriteLine("[{0},{1}]", m.Data[0], m.Data[1])
            };

            var synchronizer = new TopicSynchronizer<int, MockMessage, IList<MockMessage>>(
                subscriber, subscription, cache,
                s => snapshotClient,
                (newValue, cachedValue) => newValue.Data[1] > cachedValue.Data[1],
                m => m.Data[1] >= 10,
                m => m.Data[0],
                x => x);


            Assert.IsTrue(cache.IsEmpty);

            Task.Run(() =>
            {
                for (byte i = 50; i < 250; i++)
                {
                    subscriber.InvokeSubscriptionAccessor("", new byte[] {1, i});
                    Thread.Sleep(10);
                }
            });

            synchronizer.Init();
            Assert.AreEqual(3, cache.Count);
            subscriber.InvokeSubscriptionAccessor("", new byte[] {1, 10});
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache[1].Data[1] >= 50);
            subscriber.InvokeSubscriptionAccessor("", new byte[] {1, 9});
            Assert.IsTrue(cache[1].Data[1] >= 50);
        }

        [TestMethod]
        public void Test_Snapshot_Ahead_Stream_EmptyCache()
        {
            var cache = new DefaultLastValueCache<int, MockMessage>();
            var subscriber = new MockTopicSubscriber();
            var snapshotClient = new MockSnapshotClient
            {
                Data = new[]
                {
                    new MockMessage {Data = new byte[] {1, 11}},
                    new MockMessage {Data = new byte[] {2, 22}},
                    new MockMessage {Data = new byte[] {3, 33}}
                }
            };

            var subscription = new TopicSubscription<MockMessage>
            {
                Topic = "",
                Deserializer = m => new MockMessage {Data = m},
                MessageHandler = m => Debug.WriteLine("[{0},{1}]", m.Data[0], m.Data[1])
            };

            var synchronizer = new TopicSynchronizer<int, MockMessage, IList<MockMessage>>(
                subscriber, subscription, cache,
                s => snapshotClient,
                (newValue, cachedValue) => newValue.Data[1] > cachedValue.Data[1],
                m => m.Data[1] >= 10,
                m => m.Data[0],
                x => x);


            Assert.IsTrue(cache.IsEmpty);
            synchronizer.Init();
            Assert.AreEqual(3, cache.Count);
            subscriber.InvokeSubscriptionAccessor("", new byte[] {1, 10});
            Assert.AreEqual(3, cache.Count);
            Assert.AreEqual(11, cache[1].Data[1]);
            subscriber.InvokeSubscriptionAccessor("", new byte[] {1, 9});
            Assert.AreEqual(11, cache[1].Data[1]);
        }
    }
}