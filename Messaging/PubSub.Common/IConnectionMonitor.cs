using System;

namespace ServiceBlocks.Messaging.Common
{
    public interface IConnectionMonitor
    {
        bool IsConnected { get; set; }
        void MonitorConnectionState(Action<IConnectionMonitor, bool> onConnectionStateChanged);
    }
}