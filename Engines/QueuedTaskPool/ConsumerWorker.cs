using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBlocks.Engines.QueuedTaskPool
{
    public sealed class ConsumerWorker<TKey, TItem> : IWorker<TKey, TItem>, IDisposable
    {
        private readonly TaskCreationOptions _creationOptions;
        private readonly TaskFactory _taskFactory;
        private readonly ManualResetEventSlim _taskWaitHandle = new ManualResetEventSlim(false);
        private int _completed = 1;
        private Action<TKey, TItem> _consumeAction;
        private Action<ConsumerWorker<TKey, TItem>> _onCompleted;
        private Action<Exception> _onErrorAction;
        private Action<IWorker<TKey, TItem>> _onStopped;
        private IQueue<TKey, TItem> _queue;

        private int _running;
        private CancellationTokenSource _tokenSource;

        public ConsumerWorker(TaskFactory factory = null, TaskCreationOptions creationOptions = TaskCreationOptions.None)
        {
            _taskFactory = factory ?? new TaskFactory(TaskScheduler.Default);
            _creationOptions = creationOptions;
        }

        public int Id
        {
            get { return GetHashCode(); }
        }

        public bool IsRunning
        {
            get { return _running != 0; }
        }

        public TKey Key { get; private set; }

        public void Run(IQueue<TKey, TItem> queue, Action<IWorker<TKey, TItem>> onCompleted)
        {
            if (queue == null) throw new ArgumentNullException("queue");
            Debug.WriteLine("Reset. Old:{0} New:{1} HashCode:{2}", Key, queue.Key, GetHashCode());
            if (Interlocked.CompareExchange(ref _completed, 0, 1) == 1)
            {
                // Debug.WriteLine(string.Format("Completed Set. Method:{0} Value:{1}", "Restart", _completed));
                _onCompleted = onCompleted;
                Key = queue.Key;
                Interlocked.Exchange(ref _queue, queue);
                StartTask();
                _taskWaitHandle.Set();
            }
            else
                throw new ApplicationException("Illegal reset on active worker");
        }

        public void Stop(Action<IWorker<TKey, TItem>> onStopped = null)
        {
            // Debug.WriteLine(string.Format("Stop Called. Key:{0} Worker:{1}", Key, Id));
            Interlocked.Exchange(ref _onStopped, onStopped);
            if (_tokenSource != null) _tokenSource.Cancel();
            _taskWaitHandle.Set();
            Debug.WriteLine("Stop Done. Key:{0} Worker:{1}", Key, Id);
        }

        public ConsumerWorker<TKey, TItem> Init(Action<TKey, TItem> consumeAction,
            Action<Exception> onErrorAction = null)
        {
            if (consumeAction == null)
                throw new ArgumentNullException("consumeAction",
                    "consumeAction cannot be null. Please provide a valid delegate.");

            //interlock allows switching in runtime
            Interlocked.Exchange(ref _onErrorAction, onErrorAction);
            Interlocked.Exchange(ref _consumeAction, consumeAction);

            return this; //chain for fluent syntax
        }

        private void StartTask()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
            {
                Interlocked.Exchange(ref _tokenSource, new CancellationTokenSource());
                Task task =
                    _taskFactory.StartNew(Process, _tokenSource.Token, _creationOptions, _taskFactory.Scheduler)
                        .ContinueWith(t =>
                        {
                            Interlocked.Exchange(ref _running, 0);
                            if (_onStopped != null) _onStopped(this);
                            Debug.WriteLine("Task Completed: {0} TID:{1} Worker:{2}", DateTime.UtcNow, t.Id,
                                GetHashCode());
                        });
                Debug.WriteLine("Task Started: {0} TID:{1} Worker:{2}", DateTime.UtcNow, task.Id, GetHashCode());
            }
        }

        private void Process()
        {
            try
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    TItem item;
                    while (_queue != null && _queue.TryDequeue(out item))
                    {
                        try
                        {
                            _consumeAction(Key, item);
                        }
                        catch (Exception ex)
                        {
                            if (_onErrorAction != null)
                                _onErrorAction(ex);
                            else
                                throw;
                        }
                    }

                    if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                    {
                        Interlocked.Exchange(ref _queue, null);
                        _taskWaitHandle.Reset();
                        _onCompleted(this);
                        _taskWaitHandle.Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                if (_onErrorAction != null)
                    _onErrorAction(ex);
                _onCompleted(this);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}