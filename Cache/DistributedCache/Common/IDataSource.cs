namespace ServiceBlocks.DistributedCache.Common
{
    public interface IDataSource<TKey, TValue>
    {
        bool TryGetValue(TKey key, out TValue value);
        //TODO: consider supporting bulk loading /preloading based on some filter defined in config/policy
        //IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(); 
    }
}