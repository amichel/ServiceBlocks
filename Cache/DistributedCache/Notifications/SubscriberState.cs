using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBlocks.DistributedCache.Notifications
{
    public class SubscriberState
    {
        private Queue<Action<string>> _subscribers = new Queue<Action<string>>(8);

        public void Subscribe(Action<string> action)
        {
            _subscribers.Enqueue(action);
        }

        public void Invoke(string key)
        {
            var newQueue = new Queue<Action<string>>(Math.Max(_subscribers.Count, 8));

            while (_subscribers.Count > 0)
            {
                var callback = _subscribers.Dequeue();
                if (callback != null)
                {
                    try
                    {
                        callback(key);
                        newQueue.Enqueue(callback);
                    }
                    catch { }
                }
            }
            _subscribers = newQueue;
        }
    }
}
