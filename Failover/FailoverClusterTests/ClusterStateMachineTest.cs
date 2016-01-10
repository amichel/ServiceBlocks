using System;
using Automatonymous;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Failover.FailoverCluster;
using ServiceBlocks.Failover.FailoverCluster.StateMachine;

namespace ServiceBlocks.Failover.FailoverClusterTests
{
    [TestClass]
    public class ClusterStateMachineTest
    {
        [TestMethod]
        public void Test_Start_When_Initial()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState);

            Assert.AreEqual("Initial", stateChanged.Previous.Name);
            Assert.AreEqual("Connecting", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_Primary_Found_Passive_Backup()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState);

            var remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Passive };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Active", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_Primary_Found_Active_Backup()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState);

            var remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Active };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Passive", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_Backup_Still_Connecting()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState);

            var remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Initial };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Connecting };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            Assert.AreEqual("Initial", stateChanged.Previous.Name);
            Assert.AreEqual("Connecting", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_Primary_BecomeStandAlone_WhenInitialConnectToBackup_Fails()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState, becomeStandAloneWhenPrimaryOnInitialConnectionTimeout: true);
            
            machine.RaiseEvent(localState, machine.LostPartner, localState);

            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Active", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_Backup_Found_Primary()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Backup, out localState);

            var remoteState = new NodeState { Role = NodeRole.Primary, Status = NodeStatus.Connecting };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Passive", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_InvalidTopology()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterException cex = null;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState,
                ex => cex = ex);

            var remoteState = new NodeState { Role = NodeRole.Primary, Status = NodeStatus.Connecting };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);
            Assert.AreEqual(ClusterFailureReason.InvalidTopology, cex.Reason);
            Assert.AreEqual("Stopped", stateChanged.Previous.Name);
            Assert.AreEqual("Final", stateChanged.Current.Name);

            machine = CreateClusterStateMachine(observer, NodeRole.Backup, out localState, ex => cex = ex);

            remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Connecting };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);
            Assert.AreEqual(ClusterFailureReason.InvalidTopology, cex.Reason);
            Assert.AreEqual("Stopped", stateChanged.Previous.Name);
            Assert.AreEqual("Final", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_SplitBrain_Backup()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterException cex = null;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Backup, out localState,
                ex => cex = ex);
            var newState = new InstanceState
            {
                CurrentState = machine.Active,
                Role = NodeRole.Backup,
                Status = NodeStatus.Active
            };
            machine.TransitionToState(newState, machine.Active);

            var remoteState = new NodeState { Role = NodeRole.Primary, Status = NodeStatus.Active };
            machine.RaiseEvent(newState, machine.PartnerStatusReceived, remoteState);

            Assert.AreEqual(ClusterFailureReason.SplitBrain, cex.Reason);
            Assert.AreEqual("Stopped", stateChanged.Previous.Name);
            Assert.AreEqual("Final", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_SplitBrain_Primary()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterException cex = null;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState,
                ex => cex = ex);

            var remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Passive };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);
            remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Active };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);
            Assert.IsNull(cex);
            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Active", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_LostPartner_During_Passive_Should_Failover()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterException cex = null;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Backup, out localState,
                ex => cex = ex);

            var remoteState = new NodeState { Role = NodeRole.Primary, Status = NodeStatus.Connecting };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            machine.RaiseEvent(localState, machine.LostPartner, localState);
            Assert.AreEqual(ClusterFailureReason.LostPartner, cex.Reason);
            Assert.AreEqual("Passive", stateChanged.Previous.Name);
            Assert.AreEqual("Active", stateChanged.Current.Name);
        }

        [TestMethod]
        public void Test_LostPartner_During_Any_Should_LogException()
        {
            StateChanged<InstanceState> stateChanged = null;
            var observer = new MockStateObserver(s => stateChanged = s, Assert.IsNull);
            InstanceState localState;
            ClusterException cex = null;
            ClusterStateMachine machine = CreateClusterStateMachine(observer, NodeRole.Primary, out localState,
                ex => cex = ex);

            var remoteState = new NodeState { Role = NodeRole.Backup, Status = NodeStatus.Passive };
            machine.RaiseEvent(localState, machine.PartnerStatusReceived, remoteState);

            machine.RaiseEvent(localState, machine.LostPartner, localState);
            Assert.AreEqual(ClusterFailureReason.LostPartner, cex.Reason);
            Assert.AreEqual("Connecting", stateChanged.Previous.Name);
            Assert.AreEqual("Active", stateChanged.Current.Name);
        }


        private static ClusterStateMachine CreateClusterStateMachine(MockStateObserver observer, NodeRole role,
            out InstanceState localState, Action<ClusterException> clusterExceptionAction = null, bool becomeStandAloneWhenPrimaryOnInitialConnectionTimeout = false)
        {
            var machine = new ClusterStateMachine(clusterExceptionAction ?? Assert.IsNull, s => true, becomeStandAloneWhenPrimaryOnInitialConnectionTimeout);
            machine.StateChanged.Subscribe(observer);
            localState = new InstanceState { Role = role };
            machine.RaiseEvent(localState, machine.Start);
            return machine;
        }
    }
}