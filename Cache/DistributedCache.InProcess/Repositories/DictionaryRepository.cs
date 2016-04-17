using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Policy;
using ServiceBlocks.DistributedCache.Common;

namespace DistributedCache.InProcess.Repositories
{
    public class DictionaryRepository<TKey, TValue> : ICacheRepository<TKey, TValue>, ICacheNotificationsProvider<TKey>
    {
        private readonly ICacheNotificationsRouter _notificationsRouter;
        private readonly Func<TKey, string> _keySerializer;
        private readonly Func<string, TKey> _keyDeserializer;
        private readonly ConcurrentDictionary<TKey, CacheValueWrapper<TValue>> _store;

        public DictionaryRepository(ICacheNotificationsRouter notificationsRouter, Func<TKey, string> keySerializer,
            Func<string, TKey> keyDeserializer, ConcurrentDictionary<TKey, CacheValueWrapper<TValue>> store = null)
        {
            if (keySerializer == null) throw new ArgumentNullException(nameof(keySerializer));
            if (keyDeserializer == null) throw new ArgumentNullException(nameof(keyDeserializer));
            _notificationsRouter = notificationsRouter;
            _keySerializer = keySerializer;
            _keyDeserializer = keyDeserializer;
            _store = store ?? new ConcurrentDictionary<TKey, CacheValueWrapper<TValue>>();
        }

        public CacheValueWrapper<TValue> GetOrAdd(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            return _store.GetOrAdd(key, valueFactory);
        }

        public CacheValueWrapper<TValue> AddOrUpdate(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            bool wasUpdated = false;
            var result = _store.AddOrUpdate(key, valueFactory, (k, v) => { wasUpdated = true; return valueFactory(k); });
            if (wasUpdated)
                Publish(key);
            return result;
        }

        public CacheValueWrapper<TValue> GetValue(TKey key)
        {
            CacheValueWrapper<TValue> value;
            if (_store.TryGetValue(key, out value))
                return value;
            return CacheValueWrapper<TValue>.CreateMissing();
        }

        public bool ContainsKey(TKey key)
        {
            return _store.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        public bool TryRemove(TKey key)
        {
            CacheValueWrapper<TValue> value;
            var result = _store.TryRemove(key, out value);
            Publish(key);
            return result;
        }

        public void Clear()
        {
            _store.Clear();
            Publish(default(TKey));
        }

        public IRepositorySyncLock GetSyncLock(TKey key = default(TKey))
        {
            return DummyLock.Instance;
        }

        public void Subscribe(Action<TKey> onInvalidationOfKeysAction)
        {
            _notificationsRouter?.Subscribe<TValue>(k => onInvalidationOfKeysAction(_keyDeserializer(k)));
        }
        private void Publish(TKey key)
        {
            _notificationsRouter?.Publish<TValue>(_keySerializer(key));
        }
    }
}
