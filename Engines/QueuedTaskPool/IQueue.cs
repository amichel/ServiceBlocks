namespace ServiceBlocks.Engines.QueuedTaskPool
{
    public interface IQueue<TKey, TItem>
    {
        TKey Key { get; }
        bool IsBusy { get; }
        int Count { get; }
        void Add(TItem item);
        bool TryDequeue(out TItem item);
        bool TryAcceptWorker(IWorker<TKey, TItem> worker);
    }
}