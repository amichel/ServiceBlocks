using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace ServiceBlocks.CommandProcessor.Tests
{
    [TestClass]
    public class CommandTest
    {
        [TestMethod]
        public void TestExecuteCommand()
        {
            var mock = new Mock<MockCommand>();
            Thread.Sleep(1500);
            mock.Object.Execute();
            mock.Protected().Verify("ExecuteCommand", Times.Once());
            Assert.IsTrue(mock.Object.CreatedTime > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(mock.Object.ExecuteStartedTime > mock.Object.CreatedTime);
            Assert.IsTrue(mock.Object.ExecuteCompletedTime > mock.Object.ExecuteStartedTime);
        }

        [TestMethod]
        public void TestExecuteCommandAsync()
        {
            var mock = new Mock<MockCommand>(true);
            Thread.Sleep(1000);
            Task<bool> completed = mock.Object.Completed();
            mock.Object.Execute();
            bool result = completed.Wait(500);
            mock.Protected().Verify("ExecuteCommand", Times.Once());
            Assert.IsTrue(result);
        }
    }
}