using System;
using System.Threading;

namespace ServiceBlocks.Messaging.Common
{
    public class DefaultConnectionMonitor : IConnectionMonitor
    {
        private volatile bool _isConnected;
        private Action<IConnectionMonitor, bool> _onConnectionStateChanged;

        public DefaultConnectionMonitor(Action<IConnectionMonitor, bool> onConnectionStateChanged = null)
        {
            MonitorConnectionState(onConnectionStateChanged);
        }

        public void MonitorConnectionState(Action<IConnectionMonitor, bool> onConnectionStateChanged)
        {
            Interlocked.Exchange(ref _onConnectionStateChanged, onConnectionStateChanged);
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                if (_onConnectionStateChanged != null)
                    _onConnectionStateChanged(this, value);
            }
        }
    }
}