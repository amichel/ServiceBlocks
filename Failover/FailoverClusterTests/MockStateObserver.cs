using System;
using Automatonymous;
using ServiceBlocks.Failover.FailoverCluster.StateMachine;

namespace ServiceBlocks.Failover.FailoverClusterTests
{
    public class MockStateObserver : IObserver<StateChanged<InstanceState>>
    {
        private readonly Action _onCompletedAction;
        private readonly Action<Exception> _onError;
        private readonly Action<StateChanged<InstanceState>> _onNextAction;


        public MockStateObserver(Action<StateChanged<InstanceState>> onNextAction, Action<Exception> onError = null,
            Action onCompletedAction = null)
        {
            _onNextAction = onNextAction;
            _onError = onError ?? (ex => { });
            _onCompletedAction = onCompletedAction ?? (() => { });
        }

        public void OnNext(StateChanged<InstanceState> value)
        {
            _onNextAction(value);
        }

        public void OnError(Exception error)
        {
            _onError(error);
        }

        public void OnCompleted()
        {
            _onCompletedAction();
        }
    }
}