namespace ServiceBlocks.Messaging.Common
{
    public interface ILastValueCache<in TKey, TValue>
    {
        TValue this[TKey key] { get; set; }
        bool TryGetValue(TKey key, out TValue value);
        void UpdateValue(TKey key, TValue value);
    }
}