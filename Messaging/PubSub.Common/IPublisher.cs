using System;

namespace ServiceBlocks.Messaging.Common
{
    public interface IPublisher
    {
        void Publish(string topic, byte[] data);
        void Publish<T>(T item, Func<T, byte[]> serializer);
        void Publish<T>(string topic, T item, Func<T, byte[]> serializer);
    }
}