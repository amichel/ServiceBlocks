using NetMQ;
using NetMQ.Sockets;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqPusher : BufferedPublisher<NetMQMessage>
    {
        public NetMqPusher(string address)
        {
            Address = address;
        }

        private string Address { get; set; }

        protected override NetMQMessage CreateMessage(string topic, byte[] data)
        {
            return NetMqMessageExtensions.CreateMessage(topic, data);
        }

        protected override void ConsumerAction()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (DealerSocket socket = ctx.CreateDealerSocket())
                {
                    socket.Connect(Address);

                    foreach (NetMQMessage message in Queue.GetConsumingEnumerable())
                    {
                        socket.SendMessage(message);
                    }
                }
            }
        }
    }
}