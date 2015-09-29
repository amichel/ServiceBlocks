using System;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    public class MockTaskPool : TaskPool<int, int>
    {
        public MockTaskPool(Func<Action<int, int>> consumerActionFactory, Action<int, int> onAddAction = null,
            Action<Exception> onErrorAction = null,
            int maxDegreeOfParallelism = 100, bool suspendCompletedConsumer = true, bool warmupWorkersInPool = false,
            int waitForCompletionTimout = 10000)
            : base(
                consumerActionFactory, onAddAction, onErrorAction, maxDegreeOfParallelism, suspendCompletedConsumer,
                warmupWorkersInPool, waitForCompletionTimout)
        {
        }
    }
}