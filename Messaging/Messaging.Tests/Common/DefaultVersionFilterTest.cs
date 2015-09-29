using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class DefaultVersionFilterTest
    {
        [TestMethod]
        public void Test_All()
        {
            var filter = new DefaultVersionFilter<MockMessage>((m, m1) => m.Data[0] < m1.Data[0]);
            var firstMessage = new MockMessage {Data = new byte[] {111}};
            var secondMessage = new MockMessage {Data = new byte[] {222}};

            Assert.AreSame(secondMessage, filter.TakeNewerVersion(firstMessage, secondMessage));
            Assert.AreSame(secondMessage, filter.TakeNewerVersion(secondMessage, firstMessage));

            MockMessage latestValue;
            Assert.IsTrue(filter.TryTakeNewerVersion(firstMessage, secondMessage, out latestValue));
            Assert.AreSame(secondMessage, latestValue);

            Assert.IsFalse(filter.TryTakeNewerVersion(secondMessage, firstMessage, out latestValue));
            Assert.AreSame(secondMessage, latestValue);
        }
    }
}