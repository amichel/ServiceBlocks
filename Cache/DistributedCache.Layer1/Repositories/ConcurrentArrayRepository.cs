using System;
using System.Collections.Generic;
using ServiceBlocks.Collections;
using ServiceBlocks.DistributedCache.Common;

namespace DistributedCache.InProcess
{
    public class ConcurrentArrayRepository<TKey, TValue> : ConcurrentArray<CacheValueWrapper<TValue>>, ICacheRepository<TKey, TValue>
    {
        public ConcurrentArrayRepository(Func<CacheValueWrapper<TValue>, int> indexExtractor) : base(indexExtractor)
        {
        }

        public ConcurrentArrayRepository(int size, Func<CacheValueWrapper<TValue>, int> indexExtractor) : base(size, indexExtractor)
        {
        }

        public CacheValueWrapper<TValue> GetOrAdd(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            throw new NotImplementedException();
        }

        public CacheValueWrapper<TValue> AddOrUpdate(TKey key, Func<TKey, CacheValueWrapper<TValue>> valueFactory)
        {
            throw new NotImplementedException();
        }

        public CacheValueWrapper<TValue> GetValue(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(TKey key)
        {
            throw new NotImplementedException();
        }

        public IRepositorySyncLock GetSyncLock(TKey key = default(TKey))
        {
            throw new NotImplementedException();
        }
    }
}
