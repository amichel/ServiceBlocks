using System;
using System.Diagnostics;
using NetMQ;
using NetMQ.Sockets;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqPushAcceptor : BufferedSubscriber<NetMQMessage>
    {
        public NetMqPushAcceptor(string address)
        {
            Address = address;
        }

        private string Address { get; set; }

        protected override void ProducerAction()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (RouterSocket socket = ctx.CreateRouterSocket())
                {
                    socket.Bind(Address);

                    while (true)
                    {
                        NetMQMessage message = socket.ReceiveMessage();
                        if (!IsEmpty) Queue.Add(message);
                    }
                }
            }
        }

        protected override void ConsumeMessage(NetMQMessage message)
        {
            byte[] body;
            string topic;
            message.Parse(out body, out topic);
            InvokeSubscription(topic, body);
        }

        protected override void ConsumeError(Exception ex)
        {
            //TODO: inject logger/error handler
            Debug.WriteLine(ex.ToString());
        }
    }
}