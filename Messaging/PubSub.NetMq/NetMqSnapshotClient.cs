using NetMQ;
using NetMQ.Sockets;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqSnapshotClient : ISnapshotClient
    {
        public NetMqSnapshotClient(string address)
        {
            Address = address;
        }

        public string Address { get; set; }

        public byte[] GetSnapshot(string topic)
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (RequestSocket socket = ctx.CreateRequestSocket())
                {
                    socket.Connect(Address);
                    socket.Send(topic);
                    NetMQMessage message = socket.ReceiveMessage();
                    return message.Last.ToByteArray();
                }
            }
        }
    }
}