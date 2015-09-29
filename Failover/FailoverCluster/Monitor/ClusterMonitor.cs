using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Automatonymous;
using Newtonsoft.Json;
using ServiceBlocks.Failover.FailoverCluster.StateMachine;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Failover.FailoverCluster.Monitor
{
    public class ClusterMonitor : IObserver<StateChanged<InstanceState>>
    {
        private readonly Action<ClusterException> _clusterExceptionAction;
        private readonly InstanceState _localState;
        private readonly ClusterStateMachine _machine;
        private readonly int _partnerTimeout;
        private readonly int _pollingInterval;
        private readonly PubSubProxy _proxy;
        private readonly Action<NodeState> _stateChangedAction;
        private readonly Timer _timer;
        private IDisposable _subscription;

        public ClusterMonitor(NodeRole role, string localAddress, string partnerAddress,
            Action<NodeState> stateChangedAction = null, Action<ClusterException> clusterExceptionAction = null,
            IEnumerable<Func<NodeState, bool>> confirmActivationFuncs = null,
            int partnerTimeout = 500, int connectTimeout = 0)
        {
            _localState = new InstanceState {Role = role, Status = NodeStatus.Initial};
            if (role == NodeRole.StandAlone)
                return; //no need to run when standalone

            _stateChangedAction = stateChangedAction;
            _clusterExceptionAction = clusterExceptionAction;
            _machine = new ClusterStateMachine(HandleClusterException, CreateConfirmFunction(confirmActivationFuncs));
            _proxy = new PubSubProxy(localAddress, partnerAddress, OnPartnerUpdate, OnConnectionStateChanged,
                connectTimeout);

            _partnerTimeout = partnerTimeout;
            _pollingInterval = _partnerTimeout/2;
            _timer = new Timer(SendUpdateAndCheckForTimeout, null, Timeout.Infinite, _pollingInterval);
        }

        public NodeState CurrentState
        {
            get { return _localState; }
        }

        public IPublisher Publisher
        {
            get { return _proxy.Publisher; }
        }

        public ISubscriber Subscriber
        {
            get { return _proxy.Subscriber; }
        }

        public void OnNext(StateChanged<InstanceState> state)
        {
            if (_stateChangedAction != null)
                _stateChangedAction(state.Instance);

            if (state.Instance.Status == NodeStatus.Stopped)
                OnStop();
        }

        public void OnError(Exception error)
        {
            //TODO: Inject Logger
            Debug.WriteLine(error);
        }

        public void OnCompleted()
        {
            //TODO: Inject Logger
            Debug.WriteLine("State Machine Observable Completed");
        }

        private Func<NodeState, bool> CreateConfirmFunction(IEnumerable<Func<NodeState, bool>> confirmActivationFunc)
        {
            if (confirmActivationFunc == null)
                return state => true;

            return state => confirmActivationFunc.FirstOrDefault(fn => fn(state).Equals(false)) == null;
        }

        private void SendUpdateAndCheckForTimeout(object state)
        {
            _proxy.Publish(_localState);

            if (_localState.LastPartnerUpdate.HasValue &&
                DateTime.UtcNow.Subtract(_localState.LastPartnerUpdate.Value).TotalMilliseconds > _partnerTimeout)
                _machine.RaiseEvent(_localState, _machine.LostPartner, _localState);
        }

        private void OnConnectionStateChanged(bool state)
        {
            if (!state)
                _machine.RaiseEvent(_localState, _machine.LostPartner, _localState);
        }

        private void OnPartnerUpdate(NodeState remote)
        {
            _localState.LastPartnerUpdate = DateTime.UtcNow;
#if DEBUG
            LogStatusUpdate(_localState, remote);
#endif
            _machine.RaiseEvent(_localState, _machine.PartnerStatusReceived, remote);
        }

        private void LogStatusUpdate(NodeState local, NodeState remote)
        {
            Debug.WriteLine("Cluster Status Updated. Local:{0} Remote:{1}", JsonConvert.SerializeObject(local),
                JsonConvert.SerializeObject(remote));
        }

        private void HandleClusterException(ClusterException exception)
        {
            if (exception.LocalState == null)
                exception.LocalState = _localState;

            if (_clusterExceptionAction != null)
                _clusterExceptionAction(exception);

            Debug.WriteLine("Cluster Failure. Reason:{0} Local:{1} Remote:{2}", exception.Reason,
                JsonConvert.SerializeObject(exception.LocalState), JsonConvert.SerializeObject(exception.RemoteState));
        }

        public void Start()
        {
            try
            {
                _subscription = _machine.StateChanged.Subscribe(this);
                _machine.RaiseEvent(_localState, _machine.Start);

                if (!_proxy.Start())
                    throw new ClusterException(ClusterFailureReason.ConnectionTimeout, _localState, null);

                _timer.Change(0, _pollingInterval);
            }
            catch (ClusterException cex)
            {
                _machine.RaiseEvent(_localState, _machine.Stop);
                HandleClusterException(cex);
            }
        }

        public void Stop()
        {
            _machine.RaiseEvent(_localState, _machine.Stop);
        }

        private void OnStop()
        {
            _subscription.Dispose();
            _proxy.Stop();
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}