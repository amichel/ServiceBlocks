using System;
using Automatonymous;

namespace ServiceBlocks.Failover.FailoverCluster.StateMachine
{
    public class ClusterStateMachine : AutomatonymousStateMachine<InstanceState>
    {
        public ClusterStateMachine(Action<ClusterException> handleClusterExceptionAction,
            Func<NodeState, bool> confirmActivationFunc)
        {
            InstanceState(x => x.CurrentState);

            State(() => Connecting);
            State(() => Active);
            State(() => Passive);
            State(() => Stopped);

            Event(() => Start);
            Event(() => PartnerStatusReceived);
            Event(() => LostPartner);
            Event(() => Stop);

            Initially(When(Start).Then(local => local.Status = NodeStatus.Connecting).TransitionTo(Connecting));

            During(Connecting,
                //Passive Backup Partner Found
                When(PartnerStatusReceived,
                    remote => remote.Role == NodeRole.Backup && remote.Status == NodeStatus.Passive)
                    .Then((local, remote) => local.Status = NodeStatus.Active)
                    .TransitionTo(Active),
                //Active Backup Partner Found
                When(PartnerStatusReceived,
                    remote => remote.Role == NodeRole.Backup && remote.Status == NodeStatus.Active)
                    .Then((local, remote) => local.Status = NodeStatus.Passive)
                    .TransitionTo(Passive),
                //Primary Partner Found
                When(PartnerStatusReceived, remote => remote.Role == NodeRole.Primary)
                    .Then((local, remote) => local.Status = NodeStatus.Passive)
                    .TransitionTo(Passive));

            //Handle Split Brain - Primary Instance Wins
            During(Active,
                When(PartnerStatusReceived,
                    remote => remote.Status == NodeStatus.Active && remote.Role == NodeRole.Primary)
                    .Then(
                        (local, remote) =>
                            handleClusterExceptionAction(new ClusterException(ClusterFailureReason.SplitBrain, local,
                                remote)))
                    .Then((local, remote) => this.RaiseEvent(local, Stop)));

            //Handle loss of cluster partner (disconnect/timeout). Passive node becomes active
            During(Passive,
                When(LostPartner, local => confirmActivationFunc(local))
                    .Then(local => local.Status = NodeStatus.Active)
                    .TransitionTo(Active));
            DuringAny(
                When(LostPartner)
                    .Then(local => handleClusterExceptionAction(new ClusterException(ClusterFailureReason.LostPartner))));

            DuringAny(When(PartnerStatusReceived).Then((local, remote) =>
            {
                //Invalid Topology
                if (local.Role == remote.Role)
                {
                    handleClusterExceptionAction(new ClusterException(ClusterFailureReason.InvalidTopology,
                        local, remote));
                    this.RaiseEvent(local, Stop);
                }
            }));


            DuringAny(When(Stop).Then(local => local.Status = NodeStatus.Stopped).TransitionTo(Stopped).Finalize());
        }


        public State Connecting { get; private set; }
        public State Active { get; private set; }
        public State Passive { get; private set; }
        public State Stopped { get; private set; }

        public Event Start { get; private set; }
        public Event<NodeState> PartnerStatusReceived { get; private set; }
        public Event<NodeState> LostPartner { get; private set; }
        public Event Stop { get; private set; }
    }
}