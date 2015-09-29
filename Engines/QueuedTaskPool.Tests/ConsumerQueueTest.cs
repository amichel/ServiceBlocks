using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Engines.QueuedTaskPool.Tests
{
    [TestClass]
    public class ConsumerQueueTest
    {
        private ConsumerQueue<int, int> Create_And_Add(CountdownEvent dequeueCount)
        {
            var target = new ConsumerQueue<int, int>(11, w => { }, q => dequeueCount.Signal());
            target.Add(1);
            return target;
        }

        [TestMethod]
        public void CreateAnd_Add()
        {
            ConsumerQueue<int, int> target = Create_And_Add(new CountdownEvent(0));
            Assert.AreEqual(1, target.Count);
            Assert.AreEqual(11, target.Key);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void workerCompleted_Should_Throw_ArgumentNullException()
        {
            var target = new ConsumerQueue<int, int>(11, null, q => { });
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void onDequeue_Should_Throw_ArgumentNullException()
        {
            var target = new ConsumerQueue<int, int>(11, w => { }, null);
        }

        [TestMethod]
        public void Create_Add_And_Dequeue()
        {
            var dequeueCount = new CountdownEvent(1);
            ConsumerQueue<int, int> target = Create_And_Add(dequeueCount);
            int item;
            Assert.IsTrue(target.TryDequeue(out item));
            Assert.AreEqual(0, target.Count);
            Assert.AreEqual(1, item);
            Assert.IsFalse(target.TryDequeue(out item));
            Assert.AreEqual(0, target.Count);
            Assert.IsTrue(dequeueCount.Wait(10));
        }


        [TestMethod]
        public void TryAcceptWorkerMock()
        {
            var worker = new MockWorker();
            var completed = new ManualResetEventSlim(false);
            var dequeueCount = new CountdownEvent(1);
            var target = new ConsumerQueue<int, int>(11, w =>
            {
                Assert.AreSame(worker, w);
                completed.Set();
            }, q => dequeueCount.Signal()); //test that correct instance passed
            target.Add(1);
            Task task = Task.Factory.StartNew(() => target.TryAcceptWorker(worker));

            Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => target.IsBusy, 1000));
            //should become busy when accept occurs
            task.Wait();
            Assert.IsTrue(dequeueCount.Wait(1000)); //completed should be called
            Assert.IsTrue(completed.Wait(1000)); //completed should be called
        }
    }
}