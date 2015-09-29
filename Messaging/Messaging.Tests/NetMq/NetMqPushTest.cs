using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.NetMq;

namespace ServiceBlocks.Messaging.Tests.NetMq
{
    [TestClass]
    public class NetMqPushTest
    {
        [TestMethod]
        public void TestPush()
        {
            string address = string.Format("tcp://localhost:22245");

            using (var acceptor = new NetMqPushAcceptor(address))
            using (var pusher = new NetMqPusher(address))
            using (var countDown = new CountdownEvent(2))
            {
                acceptor.Start();
                acceptor.Subscribe("test", m => { if (!countDown.IsSet) countDown.Signal(); });
                pusher.Start();

                Task.Run(() =>
                {
                    while (!countDown.IsSet)
                    {
                        pusher.Publish("test", new byte[] {1, 2, 3});
                        Thread.Sleep(100);
                    }
                });

                Assert.IsTrue(countDown.Wait(5000));
            }
        }
    }
}