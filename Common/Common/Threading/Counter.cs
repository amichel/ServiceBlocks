using System;
using System.Threading;

namespace ServiceBlocks.Common.Threading
{
    [Serializable]
    public class Counter
    {
        private int _count;

        public int Count
        {
            get { return Thread.VolatileRead(ref _count); }
        }

        public Counter Increment()
        {
            Interlocked.Increment(ref _count);
            return this;
        }

        public Counter Decrement()
        {
            Interlocked.Decrement(ref _count);
            return this;
        }

        public Counter Reset()
        {
            Interlocked.Exchange(ref _count, 0);
            return this;
        }

        public bool CompareAndReset(int comparand)
        {
            return Interlocked.CompareExchange(ref _count, 0, comparand) == comparand;
        }

        public int Next()
        {
            Increment();
            return _count;
        }
    }
}