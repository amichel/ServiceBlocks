using System;
using System.Collections.Generic;

namespace ServiceBlocks.DistributedCache.Common
{
    public interface ICacheNotificationsRouter
    {
        void Subscribe<TValue>(Action<string> onInvalidationOfKeyAction);
        void Publish<TValue>(string keyToInvalidate);

        void Subscribe<TKey, TValue>(Action<TKey> onInvalidationOfKeyAction, Func<string, TKey> keyDeserializer);
        void Publish<TKey, TValue>(TKey keyToInvalidate, Func<TKey, string> keySerializer);
    }
}