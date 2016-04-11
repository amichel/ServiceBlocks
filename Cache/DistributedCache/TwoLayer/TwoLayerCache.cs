using System;
using System.Collections.Generic;
using System.Linq;
using ServiceBlocks.DistributedCache.Common;

namespace ServiceBlocks.DistributedCache.TwoLayer
{
    public class TwoLayerCache<TKey, TValue> : ICacheRepository<TKey, TValue>,
        IAutoUpdatingCacheRepository<TKey, TValue>,
        ICacheNotificationsProvider<TKey>
    {
        private readonly ICacheRepository<TKey, TValue> _layer1Repository;
        private readonly ICacheRepository<TKey, TValue> _layer2Repository;
        private readonly IDataSource<TKey, TValue> _dataSource;
        private readonly ICacheNotificationsRouter _cacheNotificationsRouter;
        private readonly ICacheNotificationsProvider<TKey> _layer1NotificationsProvider;
        private readonly ICacheNotificationsProvider<TKey> _layer2NotificationsProvider;

        public TwoLayerCache(ICacheRepository<TKey, TValue> layer1Repository,
            ICacheRepository<TKey, TValue> layer2Repository, IDataSource<TKey, TValue> dataSource,
            ICacheNotificationsRouter cacheNotificationsRouter,
            ICacheNotificationsProvider<TKey> layer1NotificationsProvider = null,
            ICacheNotificationsProvider<TKey> layer2NotificationsProvider = null)
        {
            _layer1Repository = layer1Repository;
            _layer2Repository = layer2Repository;
            _dataSource = dataSource;
            _cacheNotificationsRouter = cacheNotificationsRouter;
            _layer1NotificationsProvider = layer1NotificationsProvider;
            _layer2NotificationsProvider = layer2NotificationsProvider;

            SubscribeToCacheNotifications();
        }

        public CacheValueWrapper<TValue> GetOrAdd(TKey key,
             Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            var value = _layer1Repository.GetValue(key);
            switch (value.State)
            {
                case CacheValueState.Exists:
                case CacheValueState.NotFound:
                    return value;
                case CacheValueState.Expired:
                case CacheValueState.Missing:
                    value = _layer2Repository.GetValue(key);
                    if (value.State == CacheValueState.Exists || value.State == CacheValueState.NotFound)
                        return value;
                    return AddOrUpdate(key, valueFactory);
                default:
                    throw new NotImplementedException($"State {value.State} is not supported in this context");
            }
        }

        public CacheValueWrapper<TValue> AddOrUpdate(TKey key,
            Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            using (_layer2Repository.GetSyncLock(key))
            {
                CacheValueWrapper<TValue> value;
                if (!_layer2Repository.ContainsKey(key)) //double check lock
                    value = _layer2Repository.AddOrUpdate(key, valueFactory);
                else
                    value = _layer2Repository.GetValue(key);

                using (_layer1Repository.GetSyncLock(key))
                    _layer1Repository.AddOrUpdate(key, k => value);
                return value;
            }
        }

        public CacheValueWrapper<TValue> GetOrLoad(TKey key)
        {
            return GetOrAdd(key, LoadValue);
        }

        public CacheValueWrapper<TValue> GetValue(TKey key)
        {
            var value = _layer1Repository.GetValue(key);
            switch (value.State)
            {
                case CacheValueState.Exists:
                case CacheValueState.NotFound:
                    return value;
                case CacheValueState.Expired:
                case CacheValueState.Missing:
                    value = _layer2Repository.GetValue(key);
                    return value;
                default:
                    throw new NotImplementedException($"State {value.State} is not supported in this context");
            }
        }

        private CacheValueWrapper<TValue> LoadValue(TKey k)
        {
            var newValue = new CacheValueWrapper<TValue>();
            TValue rawValue;
            if (_dataSource.TryGetValue(k, out rawValue))
                newValue.SetValue(rawValue);
            else
                newValue.SetNotFound(); //not found value will be cached
            return newValue;
        }

        public bool ContainsKey(TKey key)
        {
            return _layer1Repository.ContainsKey(key) || _layer2Repository.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator()
        {
            //currently this can only be implemented by scanning layer 2 cache - layer 1 is missing data, so enumerating it is not reliable
            return _layer2Repository.GetEnumerator();
        }

        public bool TryRemove(TKey key)
        {
            var result = _layer2Repository.TryRemove(key) || _layer1Repository.TryRemove(key);
            Publish(key);
            return result;
        }

        public void Clear()
        {
            _layer2Repository.Clear();
            _layer1Repository.Clear();
            Publish(default(TKey));
        }

        public IRepositorySyncLock GetSyncLock(TKey key = default(TKey))
        {
            return DummyLock.Instance;
        }

        private void SubscribeToCacheNotifications()
        {
            _layer2NotificationsProvider?.Subscribe(InvalidateLayer1Keys); //if layer2 notifications are available invalidate layer 1 with them

            if (_cacheNotificationsRouter != null)
                _layer1NotificationsProvider?.Subscribe(_cacheNotificationsRouter.Publish<TKey, TValue>);
            //if layer 1 notifications available, propagate them to the router that external client may subscribe to
            //otherwise will be handled after invalidation
        }
        private void InvalidateLayer1Keys(TKey key)
        {
            _layer1Repository.TryRemove(key);
            Publish(key);
        }

        public void Subscribe(Action<TKey> onInvalidationOfKeyAction)
        {
            _cacheNotificationsRouter.Subscribe<TKey, TValue>(onInvalidationOfKeyAction);
        }

        private void Publish(TKey keyToInvalidate)
        {
            if (_layer1NotificationsProvider == null)
                _cacheNotificationsRouter?.Publish<TKey, TValue>(keyToInvalidate);
        }
    }
}