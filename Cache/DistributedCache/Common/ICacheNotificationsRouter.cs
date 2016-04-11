using System;
using System.Collections.Generic;

namespace ServiceBlocks.DistributedCache.Common
{
    public interface ICacheNotificationsRouter
    {
        void Subscribe<TKey, TValue>(Action<TKey> onInvalidationOfKeyAction);
        void Publish<TKey, TValue>(TKey keyToInvalidate);
    }
}