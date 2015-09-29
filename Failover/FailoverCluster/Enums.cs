namespace ServiceBlocks.Failover.FailoverCluster
{
    public enum NodeRole
    {
        StandAlone,
        Primary,
        Backup
    }

    public enum NodeStatus
    {
        Initial,
        Connecting,
        Active,
        Passive,
        Stopped
    }

    public enum ClusterFailureReason
    {
        InvalidTopology,
        SplitBrain,
        LostPartner,
        ConnectionTimeout
    }
}