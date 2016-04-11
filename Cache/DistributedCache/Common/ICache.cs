using System.Collections.Generic;

namespace ServiceBlocks.DistributedCache.Common
{
    public interface ICache<TKey, TValue>
    {
        CacheValueWrapper<TValue> AddOrUpdate(TKey key, out CacheValueWrapper<TValue> value);
        CacheValueWrapper<TValue> GetValue(TKey key);
        IEnumerator<KeyValuePair<TKey, CacheValueWrapper<TValue>>> GetEnumerator();
        void Clear();
    }
}
