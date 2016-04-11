using System;
using System.Collections.Generic;

namespace ServiceBlocks.DistributedCache.Common
{
    public interface IAutoUpdatingCacheRepository<TKey, TValue>
    {
        CacheValueWrapper<TValue> GetOrLoad(TKey key);
    }

    public interface ICacheRepository<TKey, TValue>
    {
        //TODO: consider Api to preload bulks of data
        CacheValueWrapper<TValue> GetOrAdd(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory);
        CacheValueWrapper<TValue> AddOrUpdate(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory);
        CacheValueWrapper<TValue> GetValue(TKey key);
        bool ContainsKey(TKey key);
        IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator();
        bool TryRemove(TKey key);
        void Clear();
        IRepositorySyncLock GetSyncLock(TKey key = default(TKey));
    }
}
