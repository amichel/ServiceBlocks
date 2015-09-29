using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.NetMq;

namespace ServiceBlocks.Messaging.Tests.NetMq
{
    [TestClass]
    public class NetMqPubSubTest
    {
        [TestMethod]
        public void TestPubSub()
        {
            string address = string.Format("tcp://localhost:22244");
            using (var publisher = new NetMqPublisher(address))
            using (var subscriber = new NetMqSubscriber(address))
            using (var countDown = new CountdownEvent(2))
            {
                publisher.Start();
                subscriber.Start();
                subscriber.Subscribe("test", m => { if (!countDown.IsSet) countDown.Signal(); });

                Task.Run(() =>
                {
                    while (!countDown.IsSet)
                    {
                        publisher.Publish("test", new byte[] {1, 2, 3});
                        Thread.Sleep(100);
                    }
                });

                Assert.IsTrue(countDown.Wait(5000));
            }
        }
    }
}