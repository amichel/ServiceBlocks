using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Failover.FailoverCluster.StateMachine;

namespace ServiceBlocks.Failover.FailoverClusterTests
{
    [TestClass]
    public class InstanceStateTest
    {
        [TestMethod]
        public void TestLastPartnerUpdate()
        {
            var state = new InstanceState();
            Assert.IsNull(state.LastPartnerUpdate);
            var date = new DateTime(2025, 10, 10, 3, 4, 5, 6);
            state.LastPartnerUpdate = date;
            Assert.AreEqual(date, state.LastPartnerUpdate);
        }
    }
}