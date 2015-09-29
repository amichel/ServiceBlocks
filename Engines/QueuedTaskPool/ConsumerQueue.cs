using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool
{
    public sealed class ConsumerQueue<TKey, TItem> : IQueue<TKey, TItem>
    {
        private readonly Action<IQueue<TKey, TItem>> _onDequeue;
        private readonly ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();
        private readonly Action<IWorker<TKey, TItem>> _workerCompleted;
        private IWorker<TKey, TItem> _currentWorker;

        public ConsumerQueue(TKey key, Action<IWorker<TKey, TItem>> workerCompleted,
            Action<IQueue<TKey, TItem>> onDequeue)
        {
            Key = key;
            if (workerCompleted == null)
                throw new ArgumentNullException("workerCompleted",
                    "workerCompleted cannot be null! Please pass a valid delegate.");
            if (onDequeue == null)
                throw new ArgumentNullException("onDequeue", "onDequeue cannot be null! Please pass a valid delegate.");

            _onDequeue = onDequeue;
            _workerCompleted = workerCompleted;
        }

        #region IQueue<TItem> Members

        public void Add(TItem item)
        {
            _queue.Enqueue(item);
        }

        public bool TryDequeue(out TItem item)
        {
            bool result = _queue.TryDequeue(out item);
            if (result) _onDequeue(this);
            return result;
        }

        public bool TryAcceptWorker(IWorker<TKey, TItem> worker)
        {
            if (Interlocked.CompareExchange(ref _currentWorker, worker, null) == null)
            {
                RunWorker(worker);
                return true;
            }
            return false;
        }

        private void RunWorker(IWorker<TKey, TItem> worker)
        {
            try
            {
                worker.Run(this, w => OnWorkerCompleted(worker));
            }
            catch
            {
                WorkerCompleted(worker);
                throw;
            }
        }

        private void OnWorkerCompleted(IWorker<TKey, TItem> worker)
        {
            if (_queue.Count > 0)
            {
                Debug.WriteLine("Worker Accepted Again After Complete. Key:{0} Queue:{1} Worker:{2}", Key, GetHashCode(),
                    worker.GetHashCode());
                RunWorker(worker);
            }
            else
            {
                Debug.WriteLine("Completed Called. Key:{0} Queue:{1} Worker:{2}", Key, GetHashCode(),
                    worker.GetHashCode());
                WorkerCompleted(worker);
            }
        }

        private void WorkerCompleted(IWorker<TKey, TItem> worker)
        {
            Interlocked.Exchange(ref _currentWorker, null);
            _workerCompleted(worker);
        }

        #endregion

        public int Id
        {
            get { return GetHashCode(); }
        }

        public TKey Key { get; private set; }

        public bool IsBusy
        {
            get { return _currentWorker != null; }
        }

        public int Count
        {
            get { return _queue.Count; }
        }
    }
}