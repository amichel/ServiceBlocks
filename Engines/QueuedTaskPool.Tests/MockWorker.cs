using System;
using System.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    public class MockWorker : IWorker<int, int>
    {
        #region IWorker<int,int> Members

        public int Key { get; private set; }

        public bool IsRunning
        {
            get { throw new NotImplementedException(); }
        }

        public void Run(IQueue<int, int> queue, Action<IWorker<int, int>> onCompleted)
        {
            Key = queue.Key;
            int item;
            queue.TryDequeue(out item);
            Thread.Sleep(200);
            onCompleted(this);
        }

        public void Stop(Action<IWorker<int, int>> onStopped = null)
        {
            onStopped(this);
        }

        #endregion
    }
}