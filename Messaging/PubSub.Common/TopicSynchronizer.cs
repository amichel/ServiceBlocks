using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ServiceBlocks.Messaging.Common
{
    public class TopicSynchronizer<TKey, TValue, TSnapshot>
        where TValue : class
        where TSnapshot : class
    {
        private readonly ILastValueCache<TKey, TValue> _cache;
        private readonly Func<TValue, TKey> _keyExtractor;
        private readonly Func<string, ISnapshotClient<TSnapshot>> _snapshotClientFactory;
        private readonly Func<TSnapshot, IEnumerable<TValue>> _snapshotParser;
        private readonly ISubscriber _subscriber;
        private readonly TopicSubscription<TValue> _topicSubscription;
        private readonly Func<TValue, bool> _validationFilter;
        private readonly Func<TValue, TValue, bool> _valueVersionComparer;

        public TopicSynchronizer(ISubscriber subscriber,
            TopicSubscription<TValue> topicSubscription,
            ILastValueCache<TKey, TValue> cache,
            Func<string, ISnapshotClient<TSnapshot>> snapshotClientFactory,
            Func<TValue, TValue, bool> valueVersionComparer, Func<TValue, bool> validationFilter,
            Func<TValue, TKey> keyExtractor, Func<TSnapshot, IEnumerable<TValue>> snapshotParser)
        {
            //TODO: check nulls (only relevant combinations)
            _cache = cache;
            _snapshotClientFactory = snapshotClientFactory;
            _valueVersionComparer = valueVersionComparer;
            _validationFilter = validationFilter;
            _keyExtractor = keyExtractor;
            _snapshotParser = snapshotParser;
            _topicSubscription = topicSubscription;
            _subscriber = subscriber;
        }

        public TopicSynchronizer(ISubscriber subscriber, TopicSubscription<TValue> topicSubscription,
            Func<string, ISnapshotClient<TSnapshot>> snapshotClientFactory,
            Func<TSnapshot, IEnumerable<TValue>> snapshotParser,
            Func<TValue, TValue, bool> valueVersionComparer, Func<TValue, bool> validationFilter,
            Func<TValue, TKey> keyExtractor)
            : this(
                subscriber, topicSubscription, new DefaultLastValueCache<TKey, TValue>(), snapshotClientFactory,
                valueVersionComparer, validationFilter, keyExtractor, snapshotParser)
        {
        }

        public TopicSynchronizer(ISubscriber subscriber, TopicSubscription<TValue> topicSubscription,
            Func<string, ISnapshotClient<TSnapshot>> snapshotClientFactory,
            Func<TValue, TValue, bool> valueVersionComparer, Func<TValue, TKey> keyExtractor,
            Func<TSnapshot, IEnumerable<TValue>> snapshotParser)
            : this(
                subscriber, topicSubscription, new DefaultLastValueCache<TKey, TValue>(), snapshotClientFactory,
                valueVersionComparer, null, keyExtractor, snapshotParser)
        {
        }

        public TopicSynchronizer(ISubscriber subscriber, TopicSubscription<TValue> topicSubscription,
            Func<TSnapshot, IEnumerable<TValue>> snapshotParser)
            : this(
                subscriber, topicSubscription, new DefaultLastValueCache<TKey, TValue>(), null, null, null, null, null)
        {
        }


        public void Init()
        {
            Unsubscribe();

            var subscription = new TopicSubscription<TValue>
            {
                Deserializer = _topicSubscription.Deserializer,
                Topic = _topicSubscription.Topic,
                MessageHandler = GenerateMessageHandler()
            };

            _subscriber.Subscribe(subscription);
            TryGetSnapshot(subscription);
        }


        private void TryGetSnapshot(TopicSubscription<TValue> subscription)
        {
            if (_snapshotClientFactory != null && _snapshotParser != null)
            {
                ISnapshotClient<TSnapshot> snapshotClient = _snapshotClientFactory(subscription.Topic);
                TSnapshot snapshot = snapshotClient.GetAndParseSnapshot(subscription.Topic);
                foreach (TValue value in _snapshotParser(snapshot))
                    subscription.MessageHandler(value);
            }
        }

        public void Unsubscribe()
        {
            _subscriber.Unsubscribe(_topicSubscription.Topic);
        }

        private Action<TValue> GenerateMessageHandler()
        {
            Action<TValue> messageHandler = _topicSubscription.MessageHandler;

            if (_validationFilter != null)
                messageHandler = ApplyVersionComparer(_topicSubscription.MessageHandler);

            if (_valueVersionComparer != null && _cache != null && _keyExtractor != null)
                messageHandler = ApplyValidationFilter(messageHandler);

            return messageHandler;
        }

        private Action<TValue> ApplyValidationFilter(Action<TValue> messageHandler)
        {
            return newValue =>
            {
                if (_validationFilter(newValue)) messageHandler(newValue);
                else
                    Debug.WriteLine("Dropped invalid message"); //TODO: pass handler action for invalid messages; };
            };
        }

        private Action<TValue> ApplyVersionComparer(Action<TValue> messageHandler)
        {
            return newValue =>
            {
                TValue cachedValue;
                TKey key = _keyExtractor(newValue);
                if (!_cache.TryGetValue(key, out cachedValue) || _valueVersionComparer(cachedValue, newValue))
                {
                    _cache.UpdateValue(key, newValue);
                    messageHandler(newValue);
                }
                else
                    Debug.WriteLine("Dropped out of sync message"); //TODO: pass handler action for dropped messages
            };
        }
    }
}