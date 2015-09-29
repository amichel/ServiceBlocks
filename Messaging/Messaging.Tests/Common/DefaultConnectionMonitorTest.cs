using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class DefaultConnectionMonitorTest
    {
        [TestMethod]
        public void Test_ConnectionStateChanges()
        {
            var target = new DefaultConnectionMonitor((cm, state) =>
            {
                Assert.IsTrue(cm.IsConnected);
                Assert.IsTrue(state);
            });
            target.IsConnected = true;
            Assert.IsTrue(target.IsConnected);
        }

        [TestMethod]
        public void Test_MonitorConnectionState()
        {
            var target = new DefaultConnectionMonitor((cm, state) =>
            {
                Assert.IsFalse(cm.IsConnected);
                Assert.IsFalse(state);
            });
            target.MonitorConnectionState((cm, state) =>
            {
                Assert.IsTrue(cm.IsConnected);
                Assert.IsTrue(state);
            });
            target.IsConnected = true;
            Assert.IsTrue(target.IsConnected);
        }
    }
}