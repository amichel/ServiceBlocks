using System;
using NetMQ;
using NetMQ.Sockets;
using ServiceBlocks.Common.Threading;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqSnapshotServer : ITaskWorker, IDisposable
    {
        private readonly Func<string, byte[]> _snapshotFactory;
        private readonly TaskWorker _worker;

        public NetMqSnapshotServer(string address, Func<string, byte[]> snapshotFactory)
        {
            //todo: check null
            _snapshotFactory = snapshotFactory;
            Address = address;
            _worker = new TaskWorker(RouterAction);
        }

        private string Address { get; set; }

        public void Dispose()
        {
            _worker.Dispose();
        }

        public void Start()
        {
            _worker.Start();
        }

        public void Stop(int timeout = -1)
        {
            _worker.Stop(timeout);
        }

        private void RouterAction()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (RouterSocket socket = ctx.CreateRouterSocket())
                {
                    socket.Bind(Address);

                    while (true)
                    {
                        NetMQMessage message = socket.ReceiveMessage();
                        string topic = message.Last.ConvertToString();
                        byte[] snapshot = _snapshotFactory(topic);

                        var response = new NetMQMessage();
                        response.Append(message.First);
                        response.AppendEmptyFrame();
                        response.Append(snapshot);

                        socket.SendMessage(response);
                    }
                }
            }
        }
    }
}