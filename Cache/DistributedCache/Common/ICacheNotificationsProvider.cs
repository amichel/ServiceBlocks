using System;
using System.Collections.Generic;

namespace ServiceBlocks.DistributedCache.Common
{
    public interface ICacheNotificationsProvider<out TKey>
    {
        void Subscribe(Action<TKey> onInvalidationOfKeyAction);
    }
}