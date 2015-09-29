using System;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Messaging.Common
{
    public interface IProducerConsumerWorker : ITaskWorker, IDisposable
    {
        void StartProducer();
        void StartConsumer();
    }
}