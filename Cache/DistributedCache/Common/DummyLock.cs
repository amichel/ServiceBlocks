namespace ServiceBlocks.DistributedCache.Common
{
    public class DummyLock : IRepositorySyncLock
    {
        public static DummyLock Instance { get; } = new DummyLock();

        public void Dispose()
        {
        }
    }
}
