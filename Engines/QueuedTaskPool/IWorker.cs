using System;

namespace ServiceBlocks.Engines.QueuedTaskPool
{
    public interface IWorker<TKey, TItem>
    {
        TKey Key { get; }
        bool IsRunning { get; }
        void Run(IQueue<TKey, TItem> queue, Action<IWorker<TKey, TItem>> onCompleted);
        void Stop(Action<IWorker<TKey, TItem>> onStopped = null);
    }
}