namespace ServiceBlocks.Failover.FailoverCluster
{
    public class NodeState
    {
        public NodeRole Role { get; set; }
        public NodeStatus Status { get; set; }
    }
}