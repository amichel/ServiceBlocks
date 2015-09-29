using System;
using System.Collections.Concurrent;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Messaging.Common
{
    public abstract class BufferedPublisher<TMessage> : ITaskWorker, IPublisher, IDisposable
    {
        protected readonly BlockingCollection<TMessage> Queue = new BlockingCollection<TMessage>();
        private readonly TaskWorker _consumerWorker;

        protected BufferedPublisher()
        {
            _consumerWorker = new TaskWorker(ConsumerAction);
        }

        public void Dispose()
        {
            _consumerWorker.Dispose();
            Queue.Dispose();
        }

        public void Publish(string topic, byte[] data)
        {
            Queue.Add(CreateMessage(topic, data));
        }

        public void Publish<T>(T item, Func<T, byte[]> serializer)
        {
            Publish(typeof (T).FullName, item, serializer);
        }

        public void Publish<T>(string topic, T item, Func<T, byte[]> serializer)
        {
            Publish(topic, serializer(item));
        }

        public void Start()
        {
            _consumerWorker.Start();
        }

        public void Stop(int timeout = -1)
        {
            _consumerWorker.Stop(timeout);
        }

        protected abstract TMessage CreateMessage(string topic, byte[] data);
        protected abstract void ConsumerAction();
    }
}