using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBlocks.Common.Threading
{
    public class TaskWorker : IDisposable, ITaskWorker
    {
        private readonly Action _taskAction;
        private readonly object _taskSyncLocker = new object();
        private int _running;
        private Task _task;
        private CancellationTokenSource _taskCancellation;

        public TaskWorker(Action taskAction)
        {
            _taskAction = taskAction;
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch
            {
            }
        }

        public void Start()
        {
            lock (_taskSyncLocker)
            {
                if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                {
                    _taskCancellation = new CancellationTokenSource();
                    Task task = Task.Factory.StartNew(_taskAction, _taskCancellation.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default).
                        ContinueWith(t => { Interlocked.Exchange(ref _running, 0); });
                    Interlocked.Exchange(ref _task, task);
                }
            }
        }

        public void Stop(int timeout = -1)
        {
            lock (_taskSyncLocker)
            {
                if (Interlocked.CompareExchange(ref _running, 0, 1) == 1
                    && _task != null)
                {
                    _taskCancellation.Cancel();
                    try
                    {
                        _task.Wait(timeout, _taskCancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
        }
    }
}