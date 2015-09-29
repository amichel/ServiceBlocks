using NetMQ;
using NetMQ.Sockets;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqPublisher : BufferedPublisher<NetMQMessage>
    {
        public NetMqPublisher(string address)
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
                using (PublisherSocket socket = ctx.CreatePublisherSocket())
                {
                    socket.Bind(Address);

                    foreach (NetMQMessage message in Queue.GetConsumingEnumerable())
                    {
                        socket.SendMessage(message);
                    }
                }
            }
        }
    }
}