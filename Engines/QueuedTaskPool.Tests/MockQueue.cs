using System;
using System.Collections.Generic;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    public class MockQueue : Queue<int>, IQueue<int, int>
    {
        #region IQueue<int,int> Members

        public int Key
        {
            get { return 1; }
        }

        public bool TryDequeue(out int item)
        {
            if (base.Count == 0)
            {
                item = 0;
                return false;
            }
            item = base.Dequeue();
            return true;
        }

        public bool TryAcceptWorker(IWorker<int, int> worker)
        {
            throw new NotImplementedException();
        }

        public bool IsBusy
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IQueue<int,int> Members

        public void Add(int item)
        {
            base.Enqueue(item);
        }

        #endregion
    }
}