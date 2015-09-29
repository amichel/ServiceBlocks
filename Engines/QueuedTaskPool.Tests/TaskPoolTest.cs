using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    /// <summary>
    ///     This is a test class for TaskPoolTest and is intended
    ///     to contain all TaskPoolTest Unit Tests
    /// </summary>
    [TestClass]
    public class TaskPoolTest
    {
        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion

        [TestMethod]
        public void Create_Add_AndWaitForCompletion_NotSuspendingWorkers()
        {
            Create_Add_AndWaitForCompletion(false);
        }

        [TestMethod]
        public void Create_Add_AndWaitForCompletion_SuspendingWorkers()
        {
            Create_Add_AndWaitForCompletion(true);
        }

        private void Create_Add_AndWaitForCompletion(bool suspendWorkers)
        {
            const int itemsTotal = 20;
            const int consumersTotal = 20;
            using (var countdownConsumer = new CountdownEvent(itemsTotal))
            using (var countdownAdd = new CountdownEvent(itemsTotal))
            {
                using (var target = new MockTaskPool(() => (k, i) =>
                {
                    Assert.AreEqual(k, i);
                    countdownConsumer.Signal();
                },
                    (k, i) =>
                    {
                        Assert.AreEqual(k, i);
                        countdownAdd.Signal();
                    },
                    Assert.IsNull, consumersTotal, suspendWorkers, false, 1000))
                {
                    Assert.AreEqual(consumersTotal, target.PoolSize);

                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < itemsTotal; i++)
                        {
                            target.Add(i, i);
                        }
                    });
                    Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => target.PendingRequests > 0, 1000));
                    Assert.IsTrue(countdownAdd.Wait(5000)); //should invoke add callback within timeout
                    Assert.IsTrue(countdownConsumer.Wait(5000)); //should invoke consumer within timeout

                    Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => target.IndexSize == itemsTotal, 1000));

                    Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => target.PoolSize == consumersTotal, 10000));

                    Assert.AreEqual(0, target.PendingRequests);
                }
            }
        }


        /// <summary>
        ///     All items should be consumed before pool is disposed
        /// </summary>
        [TestMethod]
        public void Dispose_Should_Try_ToStop_AllConsumers_Gracefully()
        {
            const int itemsTotal = 20;
            const int consumersTotal = 2;

            using (var countdownCompleted = new CountdownEvent(itemsTotal))
            using (var countdownAdd = new CountdownEvent(itemsTotal))
            {
                var target = new MockTaskPool(() => (k, i) =>
                {
                    Thread.Sleep(100);
                    countdownCompleted.Signal();
                },
                    (k, i) => { countdownAdd.Signal(); },
                    Assert.IsNull, consumersTotal, true);

                Assert.AreEqual(consumersTotal, target.PoolSize);

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < itemsTotal; i++)
                    {
                        target.Add(i, i);
                    }
                });
                Assert.IsTrue(countdownAdd.Wait(5000));
                target.Dispose();
                Assert.IsFalse(countdownCompleted.IsSet);
                Assert.IsTrue(countdownCompleted.Wait(5000));
            }
        }
    }
}