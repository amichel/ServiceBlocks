using System;
using System.Threading;
using Automatonymous;

namespace ServiceBlocks.Failover.FailoverCluster.StateMachine
{
    public class InstanceState : NodeState
    {
        private long _lastPartnerUpdate;
        public State CurrentState { get; set; }

        //This timestamp is updated and read from different threads. Should be safe.
        public DateTime? LastPartnerUpdate
        {
            get { return _lastPartnerUpdate == 0 ? default(DateTime?) : DateTime.FromBinary(_lastPartnerUpdate); }
            set { Interlocked.Exchange(ref _lastPartnerUpdate, value.GetValueOrDefault().ToBinary()); }
        }
    }
}