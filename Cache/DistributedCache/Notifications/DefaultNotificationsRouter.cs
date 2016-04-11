using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceBlocks.DistributedCache.Common;

namespace ServiceBlocks.DistributedCache.Notifications
{
    public class DefaultNotificationsRouter : ICacheNotificationsRouter
    {
        public void Subscribe<TKey, TValue>(Action<TKey> onInvalidationOfKeyAction)
        {
            throw new NotImplementedException();
        }

        public void Publish<TKey, TValue>(TKey keyToInvalidate)
        {
            throw new NotImplementedException();
        }
    }
}
