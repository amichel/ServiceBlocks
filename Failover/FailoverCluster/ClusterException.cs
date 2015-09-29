using System;

namespace ServiceBlocks.Failover.FailoverCluster
{
    public class ClusterException : Exception
    {
        public ClusterException()
        {
        }

        public ClusterException(ClusterFailureReason reason)
        {
            Reason = reason;
        }

        public ClusterException(ClusterFailureReason reason, NodeState localState, NodeState remoteState)
            : this(reason)
        {
            LocalState = localState;
            RemoteState = remoteState;
        }

        public ClusterFailureReason Reason { get; set; }

        public NodeState LocalState { get; set; }
        public NodeState RemoteState { get; set; }
    }
}