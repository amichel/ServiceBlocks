using System;

namespace ServiceBlocks.Messaging.Common
{
    public interface ISubscriber
    {
        void Subscribe(string topic, Action<byte[]> messageHandler);

        void Subscribe<T>(Action<T> messageHandler, Func<byte[], T> deSerializer)
            where T : class;

        void Subscribe<T>(TopicSubscription<T> subscription)
            where T : class;

        void Unsubscribe(string topic);
    }
}