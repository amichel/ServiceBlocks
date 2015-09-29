using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.CommandProcessor.Tests
{
    [TestClass]
    public class DefaultCommandProcessorTest
    {
        [TestMethod]
        public void TestExecuteWithCallback()
        {
            var processor = new MockCommandProcessor();
            var command = new MockCommand(true);
            var countDown = new CountdownEvent(1);
            processor.Init(Assert.IsNull);
            processor.Execute(1, command, state =>
            {
                Assert.IsFalse(state); //new state should be false
                countDown.Signal();
            });
            Assert.IsTrue(countDown.Wait(500));
        }

        [TestMethod]
        public void TestExecuteSync()
        {
            var processor = new MockCommandProcessor();
            var command = new MockCommand(true);
            processor.Init(Assert.IsNull);
            Assert.IsFalse(processor.Execute(1, command)); //new state should be false
        }

        [TestMethod]
        public void TestExecuteAsync()
        {
            var processor = new MockCommandProcessor();
            var command = new MockCommand(true);
            processor.Init(Assert.IsNull);
            bool newState = processor.ExecuteAsync(1, command).ConfigureAwait(true).GetAwaiter().GetResult();
            Assert.IsFalse(newState); //new state should be false
        }

        [TestMethod]
        public void TestExecuteAndForget()
        {
            var command = new MockCommand(true);
            var processor = new MockCommandProcessor();
            processor.Init(Assert.IsNull);
            processor.ExecuteAndForget(1, command);
            Assert.IsTrue(SpinWaitHelper.SpinWaitForCondition(() => command.CurrentState == false, 500));
                //new state should be false
        }
    }
}