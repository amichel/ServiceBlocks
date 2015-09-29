using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ServiceBlocks.Messaging.Common
{
    public abstract class TopicSubscriber : ISubscriber
    {
        private readonly ConcurrentDictionary<string, ITopicSubscription> _subscriptions =
            new ConcurrentDictionary<string, ITopicSubscription>();

        protected bool IsEmpty
        {
            get { return _subscriptions.IsEmpty; }
        }


        public void Subscribe(string topic, Action<byte[]> messageHandler)
        {
            Subscribe(new TopicSubscription<byte[]>
            {
                Topic = topic,
                MessageHandler = messageHandler,
                Deserializer = d => d
            });
        }

        public void Subscribe<T>(Action<T> messageHandler, Func<byte[], T> deSerializer)
            where T : class
        {
            Subscribe(new TopicSubscription<T>
            {
                Topic = typeof (T).FullName,
                MessageHandler = messageHandler,
                Deserializer = deSerializer
            });
        }

        public void Subscribe<T>(TopicSubscription<T> subscription)
            where T : class
        {
            if (subscription == null) throw new ArgumentNullException("subscription");

            _subscriptions[subscription.Topic] = subscription;
            SubscribeInternally(subscription.Topic);
        }

        public void Unsubscribe(string topic)
        {
            ITopicSubscription removed;
            _subscriptions.TryRemove(topic, out removed);
        }

        protected virtual void SubscribeInternally(string topic)
        {
        }

        protected IEnumerable<string> GetTopics()
        {
            return _subscriptions.Keys;
        }

        protected void InvokeSubscription(string topic, byte[] body)
        {
            ITopicSubscription subscriber;
            if (_subscriptions.TryGetValue(topic, out subscriber))
            {
                object item = subscriber.Deserializer(body);
                subscriber.MessageHandler.DynamicInvoke(item);
            }
        }
    }
}