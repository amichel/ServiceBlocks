using System;

namespace ServiceBlocks.Messaging.Common
{
    public class DefaultVersionFilter<TValue> : IVersionFilter<TValue> where TValue : class
    {
        private readonly Func<TValue, TValue, bool> _versionFilter;

        public DefaultVersionFilter(Func<TValue, TValue, bool> versionFilter)
        {
            //TODO: check null
            _versionFilter = versionFilter;
        }

        public bool TryTakeNewerVersion(TValue currentValue, TValue newValue, out TValue latestValue)
        {
            if (_versionFilter(currentValue, newValue))
            {
                latestValue = newValue;
                return true;
            }
            latestValue = currentValue;
            return false;
        }

        public TValue TakeNewerVersion(TValue currentValue, TValue newValue)
        {
            return _versionFilter(currentValue, newValue) ? newValue : currentValue;
        }
    }
}