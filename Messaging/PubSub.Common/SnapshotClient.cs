using System;

namespace ServiceBlocks.Messaging.Common
{
    public abstract class SnapshotClient<T> : ISnapshotClient, ISnapshotClient<T>
    {
        private readonly Func<byte[], T> _deserializer;

        protected SnapshotClient(Func<byte[], T> deserializer)
        {
            _deserializer = deserializer;
        }

        public abstract byte[] GetSnapshot(string topic);

        public T GetAndParseSnapshot(string topic)
        {
            return _deserializer(GetSnapshot(topic));
        }
    }
}