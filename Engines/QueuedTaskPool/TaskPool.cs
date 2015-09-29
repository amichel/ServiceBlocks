using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool
{
    public class TaskPool<TKey, TItem> : IDisposable
    {
        private readonly Func<Action<TKey, TItem>> _consumeActionFactory;

        private readonly ConcurrentDictionary<TKey, IQueue<TKey, TItem>> _index =
            new ConcurrentDictionary<TKey, IQueue<TKey, TItem>>();

        private readonly int _maxDegreeOfParallelism = 100;
        private readonly Action<TKey, TItem> _onAddAction;
        private readonly Action<Exception> _onErrorAction;

        private readonly ConcurrentDictionary<TKey, Counter> _pendingWorkIndex =
            new ConcurrentDictionary<TKey, Counter>();

        private readonly int _processPendingDegreeOfParallelism = 1;

        private readonly BlockingCollection<IWorker<TKey, TItem>> _releasedWorkers =
            new BlockingCollection<IWorker<TKey, TItem>>();

        private readonly bool _suspendedWorkersInPool = true;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int _waitForCompletionTimout;
        private readonly bool _warmupWorkersInPool = true;

        private readonly List<IWorker<TKey, TItem>> _workersList = new List<IWorker<TKey, TItem>>();
        private readonly ConcurrentBag<IWorker<TKey, TItem>> _workersPool = new ConcurrentBag<IWorker<TKey, TItem>>();
        private long _pendingRequestCounter;
        private TaskFactory _taskFactory;

        public TaskPool(Func<Action<TKey, TItem>> consumeActionFactory, Action<TKey, TItem> onAddAction = null,
            Action<Exception> onErrorAction = null,
            int maxDegreeOfParallelism = 100, bool suspendCompletedWorkers = true, bool warmupWorkersInPool = false,
            int waitForCompletionTimout = 5000, int processPendingDegreeOfParallelism = 0)
        {
            if (consumeActionFactory == null)
                throw new ArgumentNullException("consumerActionFactory",
                    "consumerActionFactory cannot be null. Please provide a valid factory function.");

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _suspendedWorkersInPool = suspendCompletedWorkers;
            _warmupWorkersInPool = warmupWorkersInPool;
            _waitForCompletionTimout = waitForCompletionTimout;
            _consumeActionFactory = consumeActionFactory;
            _onAddAction = onAddAction;
            _onErrorAction = onErrorAction ?? (ex => Debug.Write(ex.ToString()));

            if (processPendingDegreeOfParallelism == 0)
                _processPendingDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount/2d));
            else
                _processPendingDegreeOfParallelism = processPendingDegreeOfParallelism;
            _tokenSource = new CancellationTokenSource();
            StartTasks();
        }

        public int IndexSize
        {
            get { return _index.Count; }
        }

        public int PoolSize
        {
            get { return _workersPool.Count; }
        }

        public long PendingRequests
        {
            get { return _pendingRequestCounter; }
        }

        private void StartTasks()
        {
            _taskFactory = new TaskFactory(TaskScheduler.Default);

            for (int i = 0; i < _maxDegreeOfParallelism; i++)
            {
                ConsumerWorker<TKey, TItem> worker = CreateWorker();

                if (!_suspendedWorkersInPool && _warmupWorkersInPool)
                    worker.Run(new ConsumerQueue<TKey, TItem>(default(TKey), w => { }, q => { }), w => { });

                _workersList.Add(worker);
                _workersPool.Add(worker);
            }

            _taskFactory.StartNew(ProcessReleasedWorkers, TaskCreationOptions.LongRunning);
            _taskFactory.StartNew(ProcessPendingRequests, TaskCreationOptions.LongRunning);
            _taskFactory.StartNew(ProcessPendingWorkIndex, TaskCreationOptions.LongRunning);
        }

        private ConsumerWorker<TKey, TItem> CreateWorker()
        {
            return new ConsumerWorker<TKey, TItem>(_taskFactory,
                _suspendedWorkersInPool ? TaskCreationOptions.PreferFairness : TaskCreationOptions.LongRunning).
                Init(_consumeActionFactory(), _onErrorAction);
        }

        public void Add(TKey key, TItem item)
        {
            IQueue<TKey, TItem> queue = _index.GetOrAdd(key,
                k => new ConsumerQueue<TKey, TItem>(k, WorkCompleted, OnDequeue));
            queue.Add(item);
            _pendingWorkIndex.AddOrUpdate(key, new Counter().Increment(), (k, v) => v.Increment());
            Interlocked.Increment(ref _pendingRequestCounter);
            if (_onAddAction != null) _onAddAction(key, item);
        }

        private void OnDequeue(IQueue<TKey, TItem> queue)
        {
            if (queue.Count != 0) return;
            Counter counter;
            if (_pendingWorkIndex.TryGetValue(queue.Key, out counter) && counter.Decrement().Count == 0)
                _pendingWorkIndex.TryRemove(queue.Key, out counter);
            Interlocked.Decrement(ref _pendingRequestCounter);
        }

        private void ProcessPendingRequests()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                bool pendingQueuesFound = false;

                try
                {
                    if (_pendingRequestCounter > 0 && _index.Count > 0 && _workersPool.Count > 0)
                        //TODO: Test with parallel for
                        //Parallel.ForEach(_index, new ParallelOptions() { MaxDegreeOfParallelism = _processPendingDegreeOfParallelism }, (kv, state) =>
                        //{
                        foreach (var kv in _index)
                        {
                            if (!_pendingWorkIndex.ContainsKey(kv.Key))
                            {
                                IQueue<TKey, TItem> queue = kv.Value;
                                if (!queue.IsBusy && queue.Count > 0)
                                {
                                    pendingQueuesFound = true;

                                    if (!TryAssignWorkerToQueue(queue)) break;
                                }
                            }
                        }
                    //});
                }
                catch (Exception ex)
                {
                    _onErrorAction(ex);
                }
                finally
                {
                    if (_pendingRequestCounter > 0 && pendingQueuesFound && _workersPool.Count > 0)
                        Thread.Sleep(1);
                    else
                        Thread.Sleep(10);
                }
            }
        }

        private void ProcessPendingWorkIndex()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                bool pendingQueuesFound = false;

                try
                {
                    if (_pendingRequestCounter > 0 && _pendingWorkIndex.Count > 0 && _workersPool.Count > 0)
                        foreach (var kv in _pendingWorkIndex)
                        {
                            IQueue<TKey, TItem> queue;
                            if (_index.TryGetValue(kv.Key, out queue) && !queue.IsBusy && queue.Count > 0)
                            {
                                pendingQueuesFound = true;

                                if (!TryAssignWorkerToQueue(queue)) break;
                            }
                        }
                }
                catch (Exception ex)
                {
                    _onErrorAction(ex);
                }
                finally
                {
                    if ((_pendingWorkIndex.Count == 0 && !pendingQueuesFound) || _workersPool.Count == 0)
                        Thread.Sleep(1);
                }
            }
        }

        private bool TryAssignWorkerToQueue(IQueue<TKey, TItem> queue)
        {
            IWorker<TKey, TItem> worker;
            if (_workersPool.Count <= 0 || !_workersPool.TryTake(out worker))
                return false;

            bool workerSet = queue.TryAcceptWorker(worker);

            if (!workerSet)
            {
                Debug.WriteLine("Failed to switch Worker. Queue:{0} Worker:{1}", queue.GetHashCode(),
                    worker.GetHashCode());
                _workersPool.Add(worker);
            }
            else
                Debug.WriteLine("Succeeded to switch Worker. Queue:{0} Worker:{1}", queue.GetHashCode(),
                    worker.GetHashCode());

            return true;
        }

        private void WorkCompleted(IWorker<TKey, TItem> worker)
        {
            Debug.WriteLine("Worker Completed. Key:{0} Worker:{1}", worker.Key, worker.GetHashCode());
            _releasedWorkers.Add(worker);
        }

        private void ProcessReleasedWorkers()
        {
            try
            {
                foreach (var worker in _releasedWorkers.GetConsumingEnumerable(_tokenSource.Token))
                {
                    try
                    {
                        if (_suspendedWorkersInPool)
                        {
                            Debug.WriteLine("Stop Called. Key:{0} Worker:{1}", worker.Key, worker.GetHashCode());

                            worker.Stop(w =>
                            {
                                Debug.WriteLine("Stop Completed. Key:{0} Worker:{1}", worker.Key, worker.GetHashCode());
                                _workersPool.Add(w);
                            });
                        }
                        else
                            _workersPool.Add(worker);
                    }
                    catch (Exception ex)
                    {
                        _onErrorAction(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _onErrorAction(ex);
                throw;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_suspendedWorkersInPool)
            {
                IWorker<TKey, TItem> worker;
                while (_workersPool.TryTake(out worker))
                    worker.Stop();
            }
            if (_waitForCompletionTimout > 0)
                _tokenSource.CancelAfter(_waitForCompletionTimout);
            else
                _tokenSource.Cancel();
        }

        #endregion
    }
}