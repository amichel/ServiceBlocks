using System.Threading;

namespace ServiceBlocks.Common.Threading
{
    public class SafeMinMaxValue
    {
        private object m_minmaxlocker = new object();

        private int m_maxvalue = int.MinValue;
        private int m_minvalue = int.MaxValue;

        public int MaxValue { get { return m_maxvalue; } }
        public int MinValue { get { return m_minvalue; } }

        public void CheckNewValue(int value)
        {
            if(m_maxvalue < value || m_minvalue > value)
            {
                lock(m_minmaxlocker)
                {
                    if(m_maxvalue.CompareTo(value) < 0)
                    {
                        Interlocked.Exchange(ref m_maxvalue,value);
                    }

                    if(m_minvalue.CompareTo(value) > 0)
                    {
                        Interlocked.Exchange(ref m_minvalue,value);
                    }
                }
            }
        }
        
    }
}
