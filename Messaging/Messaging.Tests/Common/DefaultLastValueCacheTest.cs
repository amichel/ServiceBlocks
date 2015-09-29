using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    [TestClass]
    public class DefaultLastValueCacheTest
    {
        [TestMethod]
        public void Test_All_Methods()
        {
            ILastValueCache<int, int> cache = new DefaultLastValueCache<int, int>();
            int value;
            Assert.IsFalse(cache.TryGetValue(111, out value));
            cache.UpdateValue(111, 999);
            Assert.IsTrue(cache.TryGetValue(111, out value));
            Assert.AreEqual(999, value);
            Assert.AreEqual(999, cache[111]);
        }
    }
}