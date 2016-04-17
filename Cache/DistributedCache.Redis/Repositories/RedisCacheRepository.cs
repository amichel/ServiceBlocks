using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CachingFramework.Redis.Contracts;
using ServiceBlocks.DistributedCache.Common;
using ServiceBlocks.DistributedCache.Redis.Context;

namespace ServiceBlocks.DistributedCache.Redis.Repositories
{
    public class RedisCacheRepository<TKey, TValue> : ICacheRepository<TKey, TValue>, ICacheNotificationsProvider<TKey>
    {
        private readonly RedisContextProvider _context;
        private readonly ICacheNotificationsRouter _notificationsRouter;
        private readonly Func<TKey, string> _keySerializer;
        private readonly Func<string, TKey> _keyDeserializer;
        private readonly TimeSpan? _ttl;
        private const char RepositoryNameDelimiter = '~';
        public RedisCacheRepository(RedisContextProvider context, ICacheNotificationsRouter notificationsRouter, Func<TKey, string> keySerializer,
            Func<string, TKey> keyDeserializer, TimeSpan? ttl = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (keySerializer == null) throw new ArgumentNullException(nameof(keySerializer));
            if (keyDeserializer == null) throw new ArgumentNullException(nameof(keyDeserializer));
            _context = context;
            _notificationsRouter = notificationsRouter;
            _keySerializer = keySerializer;
            _keyDeserializer = keyDeserializer;
            _ttl = ttl;

            SubscribeToKeySpaceEvents();
        }

        private void SubscribeToKeySpaceEvents()
        {
            if (_notificationsRouter != null)
                _context.CacheContext.KeyEvents.Subscribe($"{RepositoryName}{RepositoryNameDelimiter}*", HandleKeyEvents);
        }

        private void HandleKeyEvents(string key, KeyEvent eventType)
        {
            switch (eventType)
            {
                case KeyEvent.Delete:
                case KeyEvent.Expire:
                case KeyEvent.Evicted:
                //TODO: how to handle expiration set and set (updates) properly here? These events are also fired when item is created. Need to find a way to not remove it from layer1 right after loading
                //case KeyEvent.ExpirationSet:
                //case KeyEvent.Set:
                    _notificationsRouter.Publish<TKey, TValue>(ExtractKeyFromString(key), _keySerializer);
                    break;
                default:
                    return;
            }
        }

        public CacheValueWrapper<TValue> GetOrAdd(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            return _context.CacheContext.Cache.FetchObject(GenerateKeyString(key),
                () => valueFactory(key), new[] { RepositoryName }, _ttl);
        }

        public CacheValueWrapper<TValue> AddOrUpdate(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            var value = valueFactory(key);
            _context.CacheContext.Cache.SetObject(GenerateKeyString(key), value, new[] { RepositoryName }, _ttl);
            return value;
        }

        public CacheValueWrapper<TValue> GetValue(TKey key)
        {
            CacheValueWrapper<TValue> value;
            if (_context.CacheContext.Cache.TryGetObject(GenerateKeyString(key), out value))
                return value;
            return CacheValueWrapper<TValue>.CreateMissing();
        }

        public bool ContainsKey(TKey key)
        {
            return _context.CacheContext.Cache.KeyExists(GenerateKeyString(key));
        }

        public IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator()
        {
            var keys = _context.CacheContext.Cache.GetKeysByTag(new[] { RepositoryName });
            foreach (var key in keys)
            {
                CacheValueWrapper<TValue> value;
                if (_context.CacheContext.Cache.TryGetObject(key, out value))
                    yield return new KeyValuePair<TKey, CacheValueWrapper<TValue>>(ExtractKeyFromString(key), value);
            }
        }

        public bool TryRemove(TKey key)
        {
            return _context.CacheContext.Cache.Remove(GenerateKeyString(key));
        }

        public void Clear()
        {
            var keys = _context.CacheContext.Cache.GetKeysByTag(new[] { RepositoryName });
            _context.CacheContext.Cache.Remove(keys.ToArray());
        }

        public IRepositorySyncLock GetSyncLock(TKey key = default(TKey))
        {
            return new RedSyncLock(_context, GenerateKeyString(key));
        }

        private string RepositoryName => typeof(TValue).FullName;

        private string GenerateKeyString(TKey key)
        {
            return $"{RepositoryName}{RepositoryNameDelimiter}{_keySerializer(key)}";
        }

        private TKey ExtractKeyFromString(string key)
        {
            return _keyDeserializer(key.Split(RepositoryNameDelimiter)[1]);
        }

        public void Subscribe(Action<TKey> onInvalidationOfKeyAction)
        {
            _notificationsRouter?.Subscribe<TKey, TValue>(onInvalidationOfKeyAction, _keyDeserializer);
        }
    }
}