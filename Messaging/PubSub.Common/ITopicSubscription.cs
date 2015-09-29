using System;

namespace ServiceBlocks.Messaging.Common
{
    public interface ITopicSubscription
    {
        string Topic { get; }
        Delegate MessageHandler { get; }
        Func<byte[], object> Deserializer { get; }
    }
}