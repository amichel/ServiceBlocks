using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceBlocks.Messaging.NetMq;

namespace ServiceBlocks.Messaging.Tests.NetMq
{
    [TestClass]
    public class NetMqSnapshotTest
    {
        [TestMethod]
        public void TestSnapshots()
        {
            string address = string.Format("tcp://localhost:22246");
            using (var server = new NetMqSnapshotServer(address, topic => Encoding.Unicode.GetBytes(topic)))
            {
                server.Start();
                byte[] snapshot =
                    Task.Run(() => new NetMqSnapshotClient(address).GetSnapshot("abc"))
                        .ConfigureAwait(true)
                        .GetAwaiter()
                        .GetResult();
                byte[] bytesExpected = Encoding.Unicode.GetBytes("abc");
                CollectionAssert.AreEqual(bytesExpected, snapshot);
            }
        }
    }
}