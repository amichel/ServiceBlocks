namespace ServiceBlocks.Messaging.Common
{
    public interface ISnapshotClient
    {
        byte[] GetSnapshot(string topic);
    }

    public interface ISnapshotClient<T>
    {
        T GetAndParseSnapshot(string topic);
    }
}