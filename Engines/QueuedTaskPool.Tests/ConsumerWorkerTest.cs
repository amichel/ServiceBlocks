using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    [TestClass]
    public class ConsumerWorkerTest
    {
        [TestMethod]
        public void TestCreate_Idle()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                Assert.IsFalse(target.IsRunning);
            }
        }

        [TestMethod]
        public void TestCreate_And_Run_ToCompletion()
        {
            using (var target = new ConsumerWorker<int, int>())
            using (var counter = new CountdownEvent(3))
            {
                target.Init((k, i) => { counter.Signal(); }, Assert.IsNull);

                var q = new MockQueue {1, 2};
                target.Run(q, w =>
                {
                    Assert.AreSame(w, target);
                    counter.Signal();
                });
                Assert.IsTrue(target.IsRunning);
                Assert.IsTrue(counter.Wait(1000));
            }
        }

        [TestMethod]
        public void TestCreate_Running_And_Stop()
        {
            using (var target = new ConsumerWorker<int, int>())
            using (var counter = new CountdownEvent(3))
            using (var completed = new CountdownEvent(1))
            {
                target.Init((k, i) => { counter.Signal(); }, Assert.IsNull);

                var q = new MockQueue {1, 2};
                target.Run(q, w =>
                {
                    Assert.AreSame(w, target);
                    completed.Signal();
                });
                Assert.IsTrue(target.IsRunning);
                Assert.IsTrue(completed.Wait(1000)); //stop is not safe to call before completed (by design)
                target.Stop(w =>
                {
                    Assert.AreSame(w, target);
                    counter.Signal();
                });
                Assert.IsTrue(counter.Wait(1000));
                Assert.IsFalse(target.IsRunning);
            }
        }

        [TestMethod]
        [ExpectedException(typeof (ApplicationException))]
        public void TestRun_When_Already_Running()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                target.Init((k, i) => Thread.Sleep(2000), Assert.IsNull);
                var q = new MockQueue {1};
                target.Run(q, w => { });
                Assert.IsTrue(target.IsRunning);
                target.Run(q, w => { });
            }
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void TestRun_With_Null_Queue()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                target.Init((k, i) => { }, Assert.IsNull);
                target.Run(null, w => { });
            }
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void TestInit_With_Null_Action()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                target.Init(null, Assert.IsNull);
            }
        }

        [TestMethod]
        public void TestRun_With_Exception()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                Exception newEx = null;
                target.Init((k, i) => { throw new ApplicationException("Test Exception"); }, ex => newEx = ex);

                var q = new MockQueue {1};
                target.Run(q, w => { });
                Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => newEx != null, 1000));
                Assert.IsInstanceOfType(newEx, typeof (ApplicationException));
            }
        }

        [TestMethod]
        public void TestRun_With_Exception_OnCompleted()
        {
            using (var target = new ConsumerWorker<int, int>())
            {
                Exception newEx = null;
                target.Init((k, i) => { }, ex => newEx = ex);
                var q = new MockQueue {1};
                target.Run(q, w => { throw new ApplicationException("Test Exception"); });
                Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => newEx != null, 1000));
                Assert.IsInstanceOfType(newEx, typeof (ApplicationException));
            }
        }

        [TestMethod]
        public void TestRun_With_ErroHandler_Null_Exception_Rethrow()
        {
            using (var counter = new CountdownEvent(1))
            using (var target = new ConsumerWorker<int, int>())
            {
                Exception newEx = null;
                target.Init((k, i) => { throw new ApplicationException("Test Exception"); });
                var q = new MockQueue {1};
                target.Run(q, w => { counter.Signal(); });
                Assert.IsTrue(counter.Wait(500));
            }
        }
    }
}