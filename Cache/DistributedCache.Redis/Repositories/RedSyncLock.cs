using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedLock;
using ServiceBlocks.DistributedCache.Common;
using ServiceBlocks.DistributedCache.Redis.Context;

namespace ServiceBlocks.DistributedCache.Redis.Repositories
{
    public class RedSyncLock : IRepositorySyncLock
    {
        private readonly RedisLock _lock;

        public RedSyncLock(RedisContextProvider context, string key, int expiryTimeout = 30000, int waitTimeout = 10000, int retryTimeout = 100)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _lock = context.LockFactory.Create(key, TimeSpan.FromMilliseconds(expiryTimeout),
                TimeSpan.FromMilliseconds(waitTimeout), TimeSpan.FromMilliseconds(retryTimeout));
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
