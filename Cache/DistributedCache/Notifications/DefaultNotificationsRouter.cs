using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using ServiceBlocks.DistributedCache.Common;

namespace ServiceBlocks.DistributedCache.Notifications
{
    public class DefaultNotificationsRouter : ICacheNotificationsRouter
    {
        private readonly NotificationsCommandProcessor _processor = new NotificationsCommandProcessor();
        private readonly ConcurrentDictionary<Type, SubscriberState> _subscriberState = new ConcurrentDictionary<Type, SubscriberState>();

        public DefaultNotificationsRouter()
        {
            _processor.Init(ex => Debug.WriteLine(ex)); //TODO: inject Serilog
        }

        public void Subscribe<TValue>(Action<string> action)
        {
            var key = typeof(TValue);
            var state = _subscriberState.GetOrAdd(key, k => new SubscriberState());
            var command = new SubscribeCommand(state, action);
            _processor.ExecuteAndForget(key, command);
        }

        public void Publish<TValue>(string keyToInvalidate)
        {
            var key = typeof(TValue);
            var state = _subscriberState.GetOrAdd(key, k => new SubscriberState());
            var command = new PublishCommand(state, keyToInvalidate);
            _processor.ExecuteAndForget(key, command);
        }

        public void Subscribe<TKey, TValue>(Action<TKey> action, Func<string, TKey> keyDeserializer)
        {
            Subscribe<TValue>(action == null ? (Action<string>)null : k => { action(keyDeserializer(k)); });
        }

        public void Publish<TKey, TValue>(TKey keyToInvalidate, Func<TKey, string> keySerializer)
        {
            Publish<TValue>(keySerializer(keyToInvalidate));
        }
    }
}
