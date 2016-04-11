using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceBlocks.DistributedCache.Common;

namespace DistributedCache.InProcess.Invalidation
{
    public class InProcessAsyncPubSub : ICacheNotificationsRouter //BufferedSubscriber?
    {
        public void Subscribe<TKey, TValue>(Action<TKey> onInvalidationOfKeysAction)
        {
            //typeof(TValue).AssemblyQualifiedName
            //    Type.GetTypeFromCLSID()
        }

        public void Publish<TKey, TValue>(TKey keyToInvalidate)
        {
            throw new NotImplementedException();
        }
    }
}
