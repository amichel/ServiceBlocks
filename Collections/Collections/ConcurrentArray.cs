using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Collections
{
    [Serializable()]
    public class ConcurrentArray<TValue> : IList<TValue>, IList where TValue : class
    {

        #region Privates

        private readonly Func<TValue, int> _indexExtractor;
        private readonly TValue[] _valuesArray;
        private readonly Counter _counter = new Counter();
        private readonly object _ctorlocker = new object();
        private readonly SafeMinMaxValue _minmax = new SafeMinMaxValue();

        #endregion
        /// <summary>
        /// </summary>
        /// <param name="indexExtractor">Function to extract array index from value</param>
        public ConcurrentArray(Func<TValue, int> indexExtractor)
            : this(byte.MaxValue + 1, indexExtractor)
        { }

        /// <summary>
        /// </summary>
        /// <param name="size">Maximum index for fixed array size.</param>
        /// <param name="indexExtractor">Function to extract array index from value</param>
        public ConcurrentArray(int size, Func<TValue,int> indexExtractor)
        {
            if (indexExtractor == null) throw new ArgumentNullException(nameof(indexExtractor));
            _indexExtractor = indexExtractor;
            lock (_ctorlocker)
            {
                Size = size;
                _valuesArray = new TValue[size];
            }
        }

        public int Size { get; private set; }

        private void AssignValue(TValue value, int index)
        {
            if (index >= Size)
                throw new IndexOutOfRangeException(String.Format("Index out of range.Index {0} is greater than the size of the collection {1}.", index, Size));

            if (Interlocked.Exchange<TValue>(ref _valuesArray[index], value) == null)
            {
                if (value != null)
                {
                    _counter.Increment();
                    _minmax.CheckNewValue(index);
                }
            }
            else
            {
                if (value == null) { _counter.Decrement(); }
            }
        }

        public void Add(TValue value)
        {
            AssignValue(value, _indexExtractor(value));
        }

        public void Update(TValue value)
        {
            var index = _indexExtractor(value);
            if (index >= Size)
            {
                throw new IndexOutOfRangeException(String.Format("Index out of range.Index {0} is greater than the size of the collection {1}.", index, Size));
            }

            Interlocked.Exchange(ref _valuesArray[index], value);
        }

        #region IDictionary<int,TValue> Members



        public bool ContainsItem(int index)
        {
            return _valuesArray[index] != null;
        }

        public ICollection<int> Keys
        {
            get
            {
                List<int> keys = new List<int>(_counter.Count);
                for (int i = _minmax.MinValue; i <= _minmax.MaxValue; i++)
                {
                    if (_valuesArray[i] != null) { keys.Add(i); }
                }
                return keys;
            }
        }

        public bool Remove(int index)
        {
            AssignValue(null, index);
            return true;
        }

        public bool TryGetValue(int index, out TValue value)
        {
            value = _valuesArray[index];
            return (value != null);
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>(Count);
                foreach (TValue value in this)
                {
                    values.Add(value);
                }
                return values;
            }
        }

        public TValue this[int index]
        {
            get
            {
                return _valuesArray[index];
            }
            set
            {
                AssignValue(value, index);
            }
        }

        #endregion

        #region IEnumerable<TValue> Members

        public IEnumerator<TValue> GetEnumerator()
        {
            for (int i = _minmax.MinValue; i <= _minmax.MaxValue; i++)
            {
                TValue tempvalue = _valuesArray[i];

                if (tempvalue != null)
                {
                    yield return tempvalue;
                }
            }
        }

        #endregion

        #region IList<TValue> Members

        public int IndexOf(TValue item)
        {
            int index = _indexExtractor(item);
            if (_valuesArray[_indexExtractor(item)] != null)
            {
                return index;
            }
            else
            {
                return -1;
            }
        }

        public void Insert(int index, TValue item)
        {
            if (_indexExtractor(item) != index)
                throw new ArgumentException("Item UniqueID does not match index!");

            AssignValue(item, index);
        }

        public void RemoveAt(int index)
        {
            AssignValue(null, index);
        }

        #endregion

        #region ICollection<TValue> Members

        //public void Add(TValue item)
        //{            
        //    AssignValue(item,item.UniqueID);
        //}

        public bool Contains(TValue item)
        {
            return ContainsItem(_indexExtractor(item));
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            List<TValue> values = new List<TValue>(Count);
            for (int i = _minmax.MinValue; i <= _minmax.MaxValue; i++)
            {
                TValue tempvalue = _valuesArray[i];

                if (tempvalue != null) { values.Add(tempvalue); }
            }
            values.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            return this.Remove(_indexExtractor(item));
        }

        public int Count
        {
            get { return _counter.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Clear()
        {
            for (int i = _minmax.MinValue; i <= _minmax.MaxValue; i++)
            {
                AssignValue(null, i);
            }
        }
        #endregion

        #region IList Members

        public int Add(object value)
        {
            Add(value as TValue);

            return IndexOf(value as TValue);
        }

        public bool Contains(object value)
        {
            return Contains(value as TValue);
        }

        public int IndexOf(object value)
        {
            return IndexOf(value as TValue);
        }

        public void Insert(int index, object value)
        {
            Insert(index, value as TValue);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            Remove(value as TValue);
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = value as TValue;
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            CopyTo(array as TValue[], index);
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return _ctorlocker; }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
