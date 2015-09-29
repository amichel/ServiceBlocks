using System.Collections.Concurrent;

namespace ServiceBlocks.Messaging.Common
{
    public class DefaultLastValueCache<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, ILastValueCache<TKey, TValue>
    {
        public void UpdateValue(TKey key, TValue value)
        {
            this[key] = value;
        }
    }
}