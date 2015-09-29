using System;
using System.Collections.Generic;

namespace ServiceBlocks.Failover.FailoverCluster.Monitor
{
    public class ClusterMonitorBuilder
    {
        private IEnumerable<Func<NodeState, bool>> _confirmActivationFuncs;
        private int _connectTimeout;
        private string _localAddress;
        private Action<ClusterException> _onClusterException;
        private string _partnerAddress;
        private int _partnerTimeout;
        private NodeRole _role;
        private Action _whenActive;
        private Action _whenConnecting;
        private Action _whenPassive;
        private Action _whenStopped;

        public ClusterMonitorBuilder ListenOn(string address)
        {
            _localAddress = address;
            return this;
        }

        public ClusterMonitorBuilder ConnectTo(string address)
        {
            _partnerAddress = address;
            return this;
        }

        public ClusterMonitorBuilder WithRole(NodeRole role)
        {
            _role = role;
            return this;
        }

        public ClusterMonitorBuilder WhenActive(Action action)
        {
            _whenActive = action;
            return this;
        }

        public ClusterMonitorBuilder WhenPassive(Action action)
        {
            _whenPassive = action;
            return this;
        }

        public ClusterMonitorBuilder WhenStopped(Action action)
        {
            _whenStopped = action;
            return this;
        }

        public ClusterMonitorBuilder WhenConnecting(Action action)
        {
            _whenConnecting = action;
            return this;
        }

        public ClusterMonitorBuilder TimeoutAfter(int timeout)
        {
            _partnerTimeout = timeout;
            return this;
        }

        public ClusterMonitorBuilder WaitForConnection(int timeout)
        {
            _connectTimeout = timeout;
            return this;
        }

        public ClusterMonitorBuilder OnClusterException(Action<ClusterException> action)
        {
            _onClusterException = action;
            return this;
        }

        public ClusterMonitorBuilder ConfirmActivationOfPassiveNode(
            params Func<NodeState, bool>[] confirmActivationFuncs)
        {
            _confirmActivationFuncs = confirmActivationFuncs;
            return this;
        }

        public ClusterMonitor Create(bool start = true)
        {
            if (_role == NodeRole.StandAlone)
                return CreateStandAlone();

            var monitor = new ClusterMonitor(_role, _localAddress, _partnerAddress,
                state =>
                {
                    switch (state.Status)
                    {
                        case NodeStatus.Connecting:
                            if (_whenConnecting != null) _whenConnecting();
                            break;
                        case NodeStatus.Active:
                            if (_whenActive != null) _whenActive();
                            break;
                        case NodeStatus.Passive:
                            if (_whenPassive != null) _whenPassive();
                            break;
                        case NodeStatus.Stopped:
                            if (_whenStopped != null) _whenStopped();
                            break;
                    }
                }, _onClusterException, _confirmActivationFuncs, _partnerTimeout, _connectTimeout);
            if (start) monitor.Start();
            return monitor;
        }

        public ClusterMonitor CreateStandAlone()
        {
            return new ClusterMonitor(NodeRole.StandAlone, "", "");
        }
    }
}