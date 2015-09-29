using System;
using System.Collections.Concurrent;
using System.Threading;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Messaging.Common
{
    public abstract class BufferedSubscriber<TMessage> : TopicSubscriber, IProducerConsumerWorker
    {
        protected readonly BlockingCollection<TMessage> Queue = new BlockingCollection<TMessage>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly TaskWorker _consumerWorker;
        private readonly TaskWorker _producerWorker;

        protected BufferedSubscriber()
        {
            _consumerWorker = new TaskWorker(ConsumerTask);
            _producerWorker = new TaskWorker(ProducerAction);
        }

        public void Start()
        {
            StartConsumer();
            StartProducer();
        }

        public void StartProducer()
        {
            _producerWorker.Start();
        }

        public void StartConsumer()
        {
            _consumerWorker.Start();
        }

        public void Stop(int timeout = -1)
        {
            _producerWorker.Stop(timeout);
            _cancellationTokenSource.Cancel();
            _consumerWorker.Stop(timeout);
        }

        public void Dispose()
        {
            _producerWorker.Dispose();
            Queue.CompleteAdding();
            _consumerWorker.Dispose();
        }

        private void ConsumerTask()
        {
            foreach (TMessage message in Queue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    ConsumeMessage(message);
                }
                catch (Exception ex)
                {
                    ConsumeError(ex);
                }
            }
        }

        protected abstract void ProducerAction();
        protected abstract void ConsumeMessage(TMessage message);
        protected abstract void ConsumeError(Exception ex);
    }
}